#r "../libs/FSharp.Data.dll"
#load "BookConfig.fsx"

open System
open FSharp.Data
open FSharp.Data.HtmlExtensions
open FSharp.Data.JsonExtensions
open BookConfig

type BookInfo = 
    { ReadingDate : string;
    Title : string;
    Author : string;
    Isbn : string;
    Editor : string;
    ImageUrl : string;
    Summary : string }

let private raiseIfEmpty value name = 
            if value = "" then failwith (sprintf "%s missing" name)

let Check book = 
    raiseIfEmpty book.Author "Author"
    raiseIfEmpty book.Editor "Editor"
    raiseIfEmpty book.Title "Title"
    raiseIfEmpty book.ImageUrl "ImageUrl"

let publisherID = Environment.GetEnvironmentVariable "publisher_id"
let productWebsPassword = Environment.GetEnvironmentVariable "publisher_password"
let searchProductsEndpoint = "https://product-api.affili.net/V3/productservice.svc/JSON/SearchProducts"
let fnacLivreShopId = "5262"
let fnacSearchUrl = "http://recherche.fnac.com/SearchResult/ResultList.aspx?SCat=2!1&sft=1&Search="

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
    let searchResult = HtmlDocument.Load(fnacSearchUrl + book.Isbn)
    
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
        |> Seq.tryFind (fun sec -> sec.AttributeValue("id") = "ficheResume")
        |> (fun fiche -> match fiche with
                            | None -> String.Empty
                            | Some node -> node.Elements().Item(1).InnerText().Trim()
            )
    
    { book with Title = title
                Author = author
                Editor = editor
                ImageUrl = imageUrl
                Summary = summary }

let private getInfosFromJson (book : BookInfo) = 
    let response = 
        Http.RequestString(searchProductsEndpoint, 
                            query = [ "publisherId", publisherID;
                                        "password", productWebsPassword;
                                        "ShopIds", fnacLivreShopId;
                                        "ImageScales", "OriginalImage";
                                        "fq", "EAN:0" + book.Isbn ],
                            httpMethod = "GET")
    
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

let GetBookInfo isbn startDate = 
    let isbn = isbn
    printfn "search for isbn '%s'" isbn
    let bookInfos = 
        { ReadingDate = startDate;
            Isbn = isbn;
            Title = "";
            Author = "";
            Editor = "";
            ImageUrl = "";
            Summary = "" }
    bookInfos
    |> getInfosFromJson
    |> getInfosFromHtmlIfNecessary
