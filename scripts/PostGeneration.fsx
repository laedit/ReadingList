#load "BookInfos.fsx"
#load "Utils.fsx"

open System
open System.Collections.Generic
open Utils
open BookInfos
open BookConfig
open System.IO

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

let generatePost (bookConfig : BookConfig) = 
    if bookConfig.Generated then 
        bookConfig
    else
        let book = BookInfos.GetBookInfo bookConfig
        BookInfos.Check book
        printfn "\tdata found"
        File.WriteAllText
            (sprintf "%s%s-%s.md" PostsFolder book.ReadingDate (book.Title |> Utils.Slugify), 
                sprintf template book.Title book.Author book.Isbn book.Editor 
                    (Utils.DownloadImageToSite book.ImageUrl book.Isbn ImagesFolder) book.Summary)
        new BookConfig(bookConfig.Isbn, bookConfig.Date, true)

let commitGeneratedPosts () =
    ExecProcessWithFail "git" "checkout master"
    ExecProcessWithFail "git" ("remote add sshorigin https://" + Environment.GetEnvironmentVariable("access_token") + ":x-oauth-basic@github.com/laedit/ReadingList.git")
    ExecProcessWithFail "git" "config --global credential.helper store"
    use sw = File.AppendText(Environment.GetEnvironmentVariable("USERPROFILE") + ".git-credentials")
    sw.Write("https://" + Environment.GetEnvironmentVariable("access_token") + ":x-oauth-basic@github.com");
    sw.Write("\n");
    ExecProcessWithFail "git" "config --global user.name \"Jérémie Bertrand\""
    ExecProcessWithFail "git" ("config --global user.email \"" + Environment.GetEnvironmentVariable("git_mail") + "\"")
    
    
    ExecProcessWithFail "git" " add ."
    ExecProcessWithFail "git" "commit -m \"Add new posts [skip ci]\""
    ExecProcessWithFail "git" "push sshorigin master"

// Add book to list (if book info)
let addBook(books:List<BookConfig>) =
    let isbn = Environment.GetEnvironmentVariable("isbn")
    let startDate = Environment.GetEnvironmentVariable("startDate")
    if not (isNull isbn || isNull startDate) then
        printfn "Add book '%s'" isbn
        books.Add(new BookConfig(isbn, startDate, false))
    books
