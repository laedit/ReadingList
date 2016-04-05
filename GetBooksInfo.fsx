#r "libs/SharpYaml.dll"
#r "libs/FSharp.Data.dll"

open System
open System.IO
open System.Collections.Generic
open FSharp.Data
open FSharp.Data.HtmlExtensions
open FSharp.Data.JsonExtensions

type BookInfo = 
    { ReadingDate : string
      Title : string
      Author : string
      Isbn : string
      Editor : string
      ImageUrl : string
      Summary : string }

type BookConfig = 
    struct
        val mutable Isbn : string
        val mutable Date : string
        val mutable Generated : bool
        new(isbn, date, generated) = 
            { Isbn = isbn
              Date = date
              Generated = generated }
    end

let (template : Printf.StringFormat<_>) = "---
layout: post
title: \"%s\"
author: \"%s\"
isbn: %s
editor: %s
---

![Couverture](/img/%s)%s"

module Constants = 
    let PublisherID = Environment.GetEnvironmentVariable "publisher_id"
    let ProductWebsPassword = Environment.GetEnvironmentVariable "publisher_password"
    let SearchProductsEndpoint = "https://product-api.affili.net/V3/productservice.svc/JSON/SearchProducts"
    let FnacLivreShopId = "5262"
    let FnacSearchUrl = "http://recherche.fnac.com/SearchResult/ResultList.aspx?SCat=2!1&Search="
    let PostsFolder = "site/_posts/"
    let ImagesFolder = "site/img/"

module Data = 
    let private getInfoFromHtml (specifications : HtmlNode) infoName = 
        (specifications.Descendants
             (fun descendant -> 
             descendant.Name() = "li" && (descendant.Elements().Head.Elements().Head.InnerText() = infoName)))
        |> Seq.head
        |> (fun li -> li.Elements())
        |> Seq.item 1
        |> (fun span -> span.InnerText())
    
    let private getInfosFromHtml (book : BookInfo) = 
        printfn "\tbackup search"
        let searchResult = HtmlDocument.Load(Constants.FnacSearchUrl + book.Isbn)
        
        let noResult = 
            searchResult.Descendants
                (fun element -> element.Name() = "div" && element.AttributeValue("class").Trim() = "txt_c noResults mrg_b_xlg")
        if not (Seq.isEmpty noResult) then failwith "data not found"

        let bookPage = 
            searchResult.Descendants [ "a" ]
            |> Seq.find (fun link -> link.AttributeValue("class").Trim() = "js-minifa-title")
            |> (fun link -> link.AttributeValue("href"))
            |> HtmlDocument.Load
        
        let sections = bookPage.Descendants [ "section" ]
        let specifications = sections |> Seq.find (fun sec -> sec.AttributeValue("id").Trim() = "specifications")
        let author = getInfoFromHtml specifications "Auteur"
        let editor = getInfoFromHtml specifications "Editeur"
        
        let imageUrl = 
            (bookPage.Descendants
                 (fun node -> node.Name() = "img" && node.AttributeValue("class").Trim() = "js-ProductVisuals-imagePreview"))
            |> Seq.head
            |> (fun img -> img.AttributeValue("src"))
        
        let title = 
            (bookPage.Descendants
                 (fun node -> node.Name() = "h1" && node.AttributeValue("class").Trim() = "ProductSummary-title"))
            |> Seq.head
            |> (fun h1 -> h1.Elements().Item(0).InnerText().Trim())
        
        let summary = 
            sections
            |> Seq.find (fun sec -> sec.AttributeValue("id") = "ficheResume")
            |> (fun fiche -> fiche.Elements().Item(1).InnerText().Trim())
        
        { book with Title = title
                    Author = author
                    Editor = editor
                    ImageUrl = imageUrl
                    Summary = summary }
    
    let private getInfosFromJson (book : BookInfo) = 
        let response = 
            Http.RequestString(Constants.SearchProductsEndpoint, 
                               query = [ "publisherId", Constants.PublisherID
                                         "password", Constants.ProductWebsPassword
                                         "ShopIds", Constants.FnacLivreShopId
                                         "ImageScales", "OriginalImage"
                                         "fq", "EAN:0" + book.Isbn ], httpMethod = "GET")
        
        let jsonResponse = JsonValue.Parse(response)
        match jsonResponse?ProductsSummary?Records.AsInteger() with
        | 0 -> book
        | _ -> 
            { book with Title = jsonResponse?Products.[0]?ProductName.AsString()
                        Author = 
                            jsonResponse?Products.[0]?Brand.AsString() |> (fun s -> s.Replace("(Auteur)", "").Trim())
                        Editor = jsonResponse?Products.[0]?Manufacturer.AsString()
                        ImageUrl = jsonResponse?Products.[0]?Properties.[1]?PropertyValue.AsString()
                        Summary = jsonResponse?Products.[0]?Description.AsString() }
    
    let private getInfosFromHtmlIfNecessary (book : BookInfo) = 
        match book.Title with
        | "" -> getInfosFromHtml book
        | _ -> book
    
    let getInfos (book : BookConfig) = 
        let isbn = book.Isbn
        printfn "search for isbn '%s'" isbn
        let bookInfos = 
            { ReadingDate = book.Date
              Isbn = isbn
              Title = ""
              Author = ""
              Editor = ""
              ImageUrl = ""
              Summary = "" }
        bookInfos
        |> getInfosFromJson
        |> getInfosFromHtmlIfNecessary

module Utils = 
    open System
    open System.Text
    open System.Text.RegularExpressions
    open System.Globalization
    
    // white space, em-dash, en-dash, underscore
    let private wordDelimiters = new Regex(@"[\s—–_]", RegexOptions.Compiled)
    // characters that are not valid
    let private invalidChars = new Regex(@"[^a-z0-9\-]", RegexOptions.Compiled)
    // multiple hyphens
    let private multipleHyphens = new Regex(@"-{2,}", RegexOptions.Compiled)
    
    let private removeDiacritics (stIn : string) = 
        let stFormD = stIn.Normalize(NormalizationForm.FormD)
        let sb = new StringBuilder()
        stFormD |> Seq.iter (fun c -> 
                       let uc = CharUnicodeInfo.GetUnicodeCategory(c)
                       if uc <> UnicodeCategory.NonSpacingMark then sb.Append(c) |> ignore)
        sb.ToString().Normalize(NormalizationForm.FormC)
    
    let slugify (value : string) = 
        value.ToLowerInvariant() // convert to lower case
        |> removeDiacritics // remove diacritics (accents)
        |> fun s -> wordDelimiters.Replace(s, "-") // ensure all word delimiters are hyphens
        |> fun s -> invalidChars.Replace(s, "") // strip out invalid characters
        |> fun s -> multipleHyphens.Replace(s, "-") // replace multiple hyphens (-) with a single hyphen
        |> fun s -> s.Trim('-') // trim hyphens (-) from ends
    
    let private raiseIfEmpty value name = 
        if value = "" then failwith (sprintf "%s missing" name)
    
    let checkBookInfos (book : BookInfo) = 
        raiseIfEmpty book.Author "Author"
        raiseIfEmpty book.Editor "Editor"
        raiseIfEmpty book.Title "Title"
        raiseIfEmpty book.ImageUrl "ImageUrl"
        raiseIfEmpty book.Summary "Summary"
    
    let downloadImageToSite imageUrl isbn = 
        let imageName = sprintf "%s%s" isbn (Path.GetExtension(imageUrl))
        let imagePath = sprintf "%s%s" Constants.ImagesFolder imageName
        File.WriteAllBytes(imagePath, (new System.Net.WebClient()).DownloadData(imageUrl))
        imageName

module Main = 
    open SharpYaml.Serialization
    
    let private generatePost (bookConfig : BookConfig) = 
        if bookConfig.Generated then 
            printfn "book '%s' already generated" bookConfig.Isbn
            bookConfig
        else 
            let book = Data.getInfos bookConfig
            Utils.checkBookInfos book
            printfn "\tdata found"
            File.WriteAllText
                (sprintf "%s%s-%s.md" Constants.PostsFolder book.ReadingDate (book.Title |> Utils.slugify), 
                 sprintf template book.Title book.Author book.Isbn book.Editor 
                     (Utils.downloadImageToSite book.ImageUrl book.Isbn) book.Summary)
            new BookConfig(bookConfig.Isbn, bookConfig.Date, true)
    
    let private getYamlSerializer = 
        let serializerSettings = new SerializerSettings()
        serializerSettings.NamingConvention <- new FlatNamingConvention()
        serializerSettings.EmitTags <- false
        new Serializer(serializerSettings)
    
    let private getBooksConfig configFileName = 
        let serializer = getYamlSerializer
        use configFile = new FileStream(configFileName, FileMode.Open)
        let books = serializer.Deserialize<List<BookConfig>>(configFile)
        books
    
    let private writeConfig configFileName booksConfig = 
        let serializer = getYamlSerializer
        use configFile = new FileStream(configFileName, FileMode.Create)
        serializer.Serialize(configFile, booksConfig)
    
    let private createFolderIfNotExists folderPath =
        if not (Directory.Exists folderPath) then
            (Directory.CreateDirectory folderPath) |> ignore

    let private cprintf c fmt = 
        Printf.kprintf 
            (fun s -> 
                let old = System.Console.ForegroundColor 
                try 
                  System.Console.ForegroundColor <- c;
                  System.Console.Write s
                finally
                  System.Console.ForegroundColor <- old) 
            fmt
    
    let cprintfn c fmt = 
        cprintf c fmt
        printfn ""

    let generatePosts configFileName = 
        printfn "start posts generation"
        let books = getBooksConfig configFileName
        let commitMessage = Environment.GetEnvironmentVariable("APPVEYOR_REPO_COMMIT_MESSAGE")
        let forcedBuild = Environment.GetEnvironmentVariable("APPVEYOR_FORCED_BUILD")
        
        if not (books <> null && books |> Seq.exists (fun book -> not book.Generated)) && 
                not (commitMessage.ToLowerInvariant().Contains("[force]") 
                || (forcedBuild <> null && forcedBuild.ToLowerInvariant() = "true"))
        then 
            cprintfn ConsoleColor.Green "no posts to generate"
            exit 42

        createFolderIfNotExists Constants.PostsFolder
        createFolderIfNotExists Constants.ImagesFolder
        let newBooksConfig = books |> Seq.map generatePost
        writeConfig configFileName (new List<BookConfig>(Seq.toArray newBooksConfig))
        printfn "end posts generation"

Main.generatePosts "books.yml"
