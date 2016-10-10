#load "Utils.fsx"
#load "BookInfos.fsx"

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
            bookConfig
        else
            let book = BookInfos.GetBookInfo bookConfig
            BookInfos.Check book
            printfn "\tdata found"
            File.WriteAllText
                (sprintf "%s%s-%s.md" PostsFolder book.ReadingDate (book.Title |> Utils.slugify), 
                 sprintf template book.Title book.Author book.Isbn book.Editor 
                     (Utils.downloadImageToSite book.ImageUrl book.Isbn ImagesFolder) book.Summary)
            new BookConfig(bookConfig.Isbn, bookConfig.Date, true)

    // forced: deploy will be executed even if there is no book page to generate
    let isDeployForced _ =
        let commitMessage = Environment.GetEnvironmentVariable("APPVEYOR_REPO_COMMIT_MESSAGE")
        let forcedBuild = Environment.GetEnvironmentVariable("APPVEYOR_FORCED_BUILD")
        (not (isNull commitMessage) && commitMessage.ToLowerInvariant().Contains("[force]") )
                || (not (isNull forcedBuild) && forcedBuild.ToLowerInvariant() = "true")

    let commitGeneratedPosts () =
        execProcessWithFail "git" "checkout master"
        execProcessWithFail "git" ("remote add sshorigin https://" + Environment.GetEnvironmentVariable("access_token") + ":x-oauth-basic@github.com/laedit/ReadingList.git")
        execProcessWithFail "git" "config --global credential.helper store"
        use sw = File.AppendText(Environment.GetEnvironmentVariable("USERPROFILE") + ".git-credentials")
        sw.Write("https://" + Environment.GetEnvironmentVariable("access_token") + ":x-oauth-basic@github.com");
        sw.Write("\n");
        execProcessWithFail "git" "config --global user.name \"Jérémie Bertrand\""
        execProcessWithFail "git" ("config --global user.email \"" + Environment.GetEnvironmentVariable("git_mail") + "\"")
        
        
        execProcessWithFail "git" " add ."
        execProcessWithFail "git" "commit -m \"Add new posts [skip ci]\""
        execProcessWithFail "git" "push sshorigin master"

    // Add book to list (if book info)
    let addBook(books:List<BookConfig>) =
        let isbn = Environment.GetEnvironmentVariable("isbn")
        let startDate = Environment.GetEnvironmentVariable("startDate")
        if not (isNull isbn || isNull startDate) then
            printfn "Add book '%s'" isbn
            books.Add(new BookConfig(isbn, startDate, false))
        books

    let generatePosts configFileName =
        let books = configFileName |> BookConfig.Load |> addBook

        printfn "start posts generation"
        let postsToGenerate = books |> Seq.exists (fun book -> not book.Generated)

        createFolderIfNotExists PostsFolder
        createFolderIfNotExists ImagesFolder
        let newBooksConfig = books |> Seq.map generatePost
        BookConfig.Write configFileName (new List<BookConfig>(Seq.toArray newBooksConfig))
        printfn "end posts generation"

        if postsToGenerate then
            commitGeneratedPosts()

        match postsToGenerate with
        | true -> Ok
        | false -> Stop "No posts to generate"

    let bakeSite () =
        System.Threading.Thread.CurrentThread.CurrentCulture <- System.Globalization.CultureInfo.CreateSpecificCulture("fr-FR")
        if execProcess @"C:\tools\Pretzel\Pretzel" "bake site" > 0 then
            Stop "Pretzel have failed to build the site"
        else
            Ok

    let private checkTaskResult taskResult isDeployForced =
        match taskResult with
        | Ok -> ()
        | Stop message -> match isDeployForced with
                            | false -> cprintfn ConsoleColor.Yellow message; Environment.Exit 0
                            | true -> ()

    let Build =
        printfn "start build"
        let isDeployForced = isDeployForced()
        if isDeployForced then cprintfn ConsoleColor.Yellow "!! Deploy forced !!"

        //  Generate page for book
        let generatePostsResult = generatePosts "books.yml"

        checkTaskResult generatePostsResult isDeployForced

        // Build website
        let bakeSiteResult = bakeSite()
        checkTaskResult generatePostsResult isDeployForced

        // Deploy
        System.Environment.SetEnvironmentVariable("PATH", ("C:\\Python35;C:\\Python35\\Scripts;" + Environment.GetEnvironmentVariable "PATH"))
        execProcessWithFail "pip" "install creep"
        execProcessWithFail "creep" @"-e ""{""""default"""": {""""connection"""": """"ftp://zlaeditn12713ne:gl%25%24%2491PN..-mj%24%23%2542@laedit.net/httpdocs/readinglist""""}}"""" -d ""{""""source"""": """"hash""""}"" -b site/_site -y"

Main.Build
