#load "BookInfos.fsx"
#load "Utils.fsx"
#r "../libs/Octokit.dll"

open System
open System.IO
open System.Linq
open System.Collections.Generic
open Utils
open BookInfos
open BookConfig

let (template : Printf.StringFormat<_>) =
    "---
layout: post
title: \"%s\"
author: \"%s\"
isbn: %s
editor: %s
---

![Couverture](/img/%s)%s"

let PostsFolder = "site/_posts/"
let ImagesFolder = "site/img/"

let GeneratePost isbn startDate =
    let book = BookInfos.GetBookInfo isbn startDate
    BookInfos.Check book
    printfn "\tdata found"
    let imageFileName = sprintf "%s%s" isbn (Path.GetExtension(book.ImageUrl))
    let imagePath = sprintf "%s%s" ImagesFolder imageFileName
    let imageContent = Utils.DownloadImage book.ImageUrl
    let postPath = sprintf "%s%s-%s.md" PostsFolder book.ReadingDate (book.Title |> Utils.Slugify)
    let postContent = sprintf template book.Title book.Author book.Isbn book.Editor imageFileName book.Summary
    (imagePath, imageContent, postPath, postContent, book.Title)

let WriteTextFile postPath postContent =
    File.WriteAllText(postPath, postContent)

let WriteNewPost postPath postContent imagePath imageContent =
    WriteTextFile postPath postContent
    File.WriteAllBytes(imagePath, imageContent)

let GetBookInfoFromEnv() =
    let isbn = Environment.GetEnvironmentVariable("isbn")
    let startDate = Environment.GetEnvironmentVariable("startDate")
    if not (isNull isbn || isNull startDate) then
        printfn "Adding book '%s' to list" isbn
        Some (isbn, startDate)
    else
        Warning "isbn and/or start date was missing to generate a new book post"
        None

let GetPostInfo isbn originalDate startDate =

    let selectPost isbn posts =
        posts
        |> Array.find (fun path -> System.IO.File.ReadLines(path).Skip(4).Take(1).First().Substring(6) = isbn)

    let reduce isbn (posts: string[]) =
        match posts.Length with
        | 0 -> failwith "not posts found"
        | 1 -> posts.[0]
        | _ -> posts |> selectPost isbn

    let replace (orig:string) (replace:string) (str:string) = str.Replace(orig, replace)

    let originalPostPath = Directory.GetFiles("site/_posts", originalDate + "*")
                           |> reduce isbn
    let postPath = originalPostPath
                   |> replace originalDate startDate // fix date in path
    let postLines = File.ReadAllLines originalPostPath
    let postContent = postLines |> String.concat Environment.NewLine // get content
    let bookTitle = postLines.[2].Substring(8, postLines.[2].Length - 9)
    (postPath, postContent, bookTitle)
