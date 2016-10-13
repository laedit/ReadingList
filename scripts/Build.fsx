#load "Utils.fsx"
#load "BookInfos.fsx"

open System
open System.IO
open System.Collections.Generic
open Utils
open BookInfos
open BookConfig

module Main = 

    type BuildConfiguration = 
        {
            IsDeployForced : bool;
            BooksFilePath : string;
            FtpUser : string;
            FtpPassword : string;
            IsTraceDebug : bool
        }

    let (template : Printf.StringFormat<_>) = "---
    layout: post
    title: \"%s\"
    author: \"%s\"
    isbn: %s
    editor: %s
    ---

    ![Couverture](/img/%s)%s"

    let PostsFolder = "site/_posts/"
    let ImagesFolder = "site/img/"

    type TaskResult = 
        | Success of BuildConfiguration
        | Failure of BuildConfiguration * string

    let bind switchFunction = 
        fun taskResult ->
            match taskResult with
            | Success s -> switchFunction s
            | Failure (conf, mess) -> match conf.IsDeployForced with
                                        | true -> switchFunction conf
                                        | false -> Failure (conf, mess)

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

    let generatePosts configuration =
        let books = configuration.BooksFilePath |> BookConfig.Load |> addBook

        printfn "start posts generation"
        let postsToGenerate = books |> Seq.exists (fun book -> not book.Generated)

        createFolderIfNotExists PostsFolder
        createFolderIfNotExists ImagesFolder
        let newBooksConfig = books |> Seq.map generatePost
        BookConfig.Write configuration.BooksFilePath (new List<BookConfig>(Seq.toArray newBooksConfig))
        printfn "end posts generation"

        if postsToGenerate then
            commitGeneratedPosts()

        match postsToGenerate with
        | true -> Success configuration
        | false -> Failure (configuration, "No posts to generate")

    let bakeSite configuration =
        // prerequisite
        execProcessWithFail "choco" "install pretzel -y"

        System.Threading.Thread.CurrentThread.CurrentCulture <- System.Globalization.CultureInfo.CreateSpecificCulture("fr-FR")
        if execProcess @"C:\tools\Pretzel\Pretzel" "bake site" > 0 then
            Failure (configuration, "Pretzel have failed to build the site")
        else
            Success configuration

    let deploySite configuration =
        // prerequisite
        System.Environment.SetEnvironmentVariable("PATH", ("C:\\Python35;C:\\Python35\\Scripts;" + Environment.GetEnvironmentVariable "PATH"))
        execProcessWithFail "pip" "install creep"

        execProcessWithFail "creep" ([@"-e ""{""""default"""": {""""connection"""": """"ftp://"; configuration.FtpUser; ":"; configuration.FtpPassword; @"@laedit.net""""}}"" -d ""{""""source"""": """"hash""""}"" -b site/_site -y"] |> Seq.fold (+) "")
        Success configuration

    let log result = 
        match result with
        | Success s -> ()
        | Failure (c, f) -> Warning f

    let warnDeployForced configuration =
        match configuration.IsDeployForced with
            | true -> Warning "!! Deploy forced !!"
            | false -> ()
        configuration

    let RunBuild =
        warnDeployForced
        >> generatePosts
        >> bind bakeSite
        >> bind deploySite
        >> log

    let Build =
        printfn "start build"

        let configuration = 
            {
                IsDeployForced = isDeployForced();
                BooksFilePath = "books.yml";
                FtpUser = Environment.GetEnvironmentVariable("ftp_user");
                FtpPassword = Environment.GetEnvironmentVariable("ftp_password");
                IsTraceDebug = false
            }

        configuration
        |> RunBuild
        |> ignore

Main.Build
