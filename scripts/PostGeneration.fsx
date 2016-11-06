#load "BookInfos.fsx"
#load "Utils.fsx"
#r "../libs/Octokit.dll"

open System
open System.Collections.Generic
open Utils
open BookInfos
open BookConfig
open System.IO
open Octokit

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

/// Création du commit via api github

let WriteNewPost postPath postContent imagePath imageContent =
    File.WriteAllText(postPath, postContent)
    File.WriteAllBytes(imagePath, imageContent)

let CommitNewPost bookTitle postPath postContent imagePath imageContent booksConfigPath booksConfigContent =
    let owner = "laedit"
    let repo = "readinglist"
    let branch = "master"
    let headMasterRef = "heads/master"

    let github = new Octokit.GitHubClient(new ProductHeaderValue("Laedit-ReadingList"))
    github.Credentials <- new Credentials(Environment.GetEnvironmentVariable("access_token"));

    let master = github.Git.Reference.Get(owner, repo, headMasterRef).Result
    let baseTree = github.Git.Commit.Get(owner, repo, master.Object.Sha).Result

    let nt = new NewTree()
    nt.BaseTree <- baseTree.Sha

    let newImageBlob = new Octokit.NewBlob()
    newImageBlob.Encoding <- EncodingType.Base64
    newImageBlob.Content <- Convert.ToBase64String(imageContent)
    let imageBlob = github.Git.Blob.Create(owner, repo, newImageBlob).Result

    let imageTreeItem = new NewTreeItem()
    imageTreeItem.Path <- imagePath
    imageTreeItem.Mode <- "100644" // new blob
    imageTreeItem.Type <- TreeType.Blob
    imageTreeItem.Sha <- imageBlob.Sha
    nt.Tree.Add(imageTreeItem)

    let postTreeItem = new NewTreeItem()
    postTreeItem.Mode <- "100644"
    postTreeItem.Type <- TreeType.Blob
    postTreeItem.Content <- postContent
    postTreeItem.Path <- postPath
    nt.Tree.Add(postTreeItem)

    let booksConfigTreeItem = new NewTreeItem()
    booksConfigTreeItem.Mode <- "100644"
    booksConfigTreeItem.Type <- TreeType.Blob
    booksConfigTreeItem.Content <- booksConfigContent
    booksConfigTreeItem.Path <- booksConfigPath
    nt.Tree.Add(booksConfigTreeItem)

    let newTree = github.Git.Tree.Create(owner, repo, nt).Result
    let newCommit = new NewCommit(sprintf "Add new book '%s' [skip ci]" bookTitle, newTree.Sha, master.Object.Sha)

    let commit = github.Git.Commit.Create(owner, repo, newCommit).Result
    github.Git.Reference.Update(owner, repo, headMasterRef, new ReferenceUpdate(commit.Sha)).Result |> ignore

let GetBookInfoFromEnv() =
    let isbn = Environment.GetEnvironmentVariable("isbn")
    let startDate = Environment.GetEnvironmentVariable("startDate")
    if not (isNull isbn || isNull startDate) then
        printfn "Adding book '%s' to list" isbn
        Some (isbn, startDate)
    else
        Warning "isbn and/or start date was missing to generate a new book post"
        None
