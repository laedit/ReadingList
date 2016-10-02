#load "Utils.fsx"
#load "BookInfos.fsx"
#load "BookConfig.fsx"

open System
open System.IO
open System.Collections.Generic
open Utils
open BookInfos
open BookConfig

let (template : Printf.StringFormat<_>) = "---
layout: post
title: \"%s\"
author: \"%s\"
isbn: %s
editor: %s
---

![Couverture](/img/%s)%s"

module Main = 

    let PostsFolder = "site/_posts/"
    let ImagesFolder = "site/img/"

    let private generatePost (bookConfig : BookConfig) = 
        if bookConfig.Generated then 
            printfn "book '%s' already generated" bookConfig.Isbn
            bookConfig
        else
            let book = BookInfos.GetBookInfo(bookConfig)
            BookInfos.Check book
            printfn "\tdata found"
            File.WriteAllText
                (sprintf "%s%s-%s.md" PostsFolder book.ReadingDate (book.Title |> Utils.slugify), 
                 sprintf template book.Title book.Author book.Isbn book.Editor 
                     (Utils.downloadImageToSite book.ImageUrl book.Isbn ImagesFolder) book.Summary)
            new BookConfig(bookConfig.Isbn, bookConfig.Date, true)

    let isBuildForced _ =
        let commitMessage = Environment.GetEnvironmentVariable("APPVEYOR_REPO_COMMIT_MESSAGE")
        let forcedBuild = Environment.GetEnvironmentVariable("APPVEYOR_FORCED_BUILD")
        (not (isNull commitMessage) && commitMessage.ToLowerInvariant().Contains("[force]") )
                || (not (isNull forcedBuild) && forcedBuild.ToLowerInvariant() = "true")

    let generatePosts configFileName isForced = 
        printfn "start posts generation"
        let books = BookConfig.Load configFileName
        let postsToGenerate = books |> Seq.exists (fun book -> not book.Generated)
        
        if not postsToGenerate
        then 
            cprintfn ConsoleColor.Green "no posts to generate"
            if isForced
            then printfn "but since the build is forced, posts are generated anyway"
            else exit 42

        createFolderIfNotExists PostsFolder
        createFolderIfNotExists ImagesFolder
        let newBooksConfig = books |> Seq.map generatePost
        BookConfig.Write configFileName (new List<BookConfig>(Seq.toArray newBooksConfig))
        printfn "end posts generation"
        postsToGenerate

    let commitGeneratedPosts () =
        execProcess "git" ("remote add sshorigin https://" + Environment.GetEnvironmentVariable("access_token") + ":x-oauth-basic@github.com/laedit/ReadingList.git")
        execProcess "git" "config --global credential.helper store"
        use sw = File.AppendText(Environment.GetEnvironmentVariable("USERPROFILE") + ".git-credentials")
        sw.Write("https://" + Environment.GetEnvironmentVariable("access_token") + ":x-oauth-basic@github.com");
        sw.Write("\n");
        execProcess "git" "config --global user.name \"Jérémie Bertrand\""
        execProcess "git" ("config --global user.email \"" + Environment.GetEnvironmentVariable("git_mail") + "\"")
        
        
        execProcess "git" " add ."
        execProcess "git" "commit -m \"Add new posts [skip ci]\""
        execProcess "git" "push sshorigin master"

    let bakeSite () =
        System.Threading.Thread.CurrentThread.CurrentCulture <- System.Globalization.CultureInfo.CreateSpecificCulture("fr-FR")
        execProcess @"C:\tools\Pretzel\Pretzel" "bake site" 

    let Build =
        printfn "start build"

        printfn "%s" (Environment.GetEnvironmentVariable("isbn"))
        printfn "%s" (Environment.GetEnvironmentVariable("startDate"))

        execProcess "git" "checkout master"
        
        let isForced = isBuildForced()
        if isForced then printfn "!! Build forced !!"

        let postsGenerated = generatePosts "books.yml" isForced
        
        if postsGenerated
        then
            commitGeneratedPosts()
        
        bakeSite()
        
Main.Build
