#load "PostGeneration.fsx"

open System
open System.Collections.Generic
open Utils
open BookInfos
open BookConfig
open PostGeneration

let generatePosts = BuildTask
                        "generate posts"
                        id
                        (fun configuration ->
                            let books = configuration.BooksFilePath |> BookConfig.Load |> addBook

                            printfn "start posts generation"
                            let postsToGenerate = books |> Seq.exists (fun book -> not book.Generated)

                            CreateFolderIfNotExists PostsFolder
                            CreateFolderIfNotExists ImagesFolder
                            let newBooksConfig = books |> Seq.map generatePost
                            BookConfig.Write configuration.BooksFilePath (new List<BookConfig>(Seq.toArray newBooksConfig))
                            printfn "end posts generation"

                            if postsToGenerate then
                                commitGeneratedPosts()

                            match postsToGenerate with
                            | true -> Success configuration
                            | false -> Failure (configuration, "No posts to generate")
                        )

let bakeSite = BuildTask
                "bake site"
                (fun _ ->
                    // prerequisite
                    ExecProcessWithFail "choco" "install pretzel -y"
                )
                (fun configuration ->
                    System.Threading.Thread.CurrentThread.CurrentCulture <- System.Globalization.CultureInfo.CreateSpecificCulture("fr-FR")
                    if ExecProcess @"C:\tools\Pretzel\Pretzel" "bake site" > 0 then
                        Failure (configuration, "Pretzel have failed to build the site")
                    else
                        Success configuration
                )

let deploySite = BuildTask
                    "deploy" 
                    (fun _ -> 
                        // prerequisite
                        System.Environment.SetEnvironmentVariable("PATH", ("C:\\Python35;C:\\Python35\\Scripts;" + Environment.GetEnvironmentVariable "PATH"))
                        ExecProcessWithFail "pip" "install creep"
                    ) 
                    (fun configuration -> 
                        ExecProcessWithFail "creep" ([@"-e ""{""""default"""": {""""connection"""": """"ftp://"; configuration.FtpUser; ":"; configuration.FtpPassword; @"@laedit.net""""}}"" -d ""{""""source"""": """"hash""""}"" -b site/_site -y"] |> Seq.fold (+) "")
                        Success configuration
                    )

let warnDeployForced configuration =
    match configuration.IsDeployForced with
        | true -> Warning "!! Deploy forced !!"
        | false -> ()
    Success configuration

let log result = 
    match result with
    | Success s -> ()
    | Failure (c, f) -> Warning f

let Build =
    warnDeployForced
    >> executeTask generatePosts
    >> executeTask bakeSite
    >> executeTask deploySite
    >> log

let configuration = 
    {
        IsDeployForced = IsDeployForced();
        BooksFilePath = "books.yml";
        FtpUser = Environment.GetEnvironmentVariable("ftp_user");
        FtpPassword = Environment.GetEnvironmentVariable("ftp_password");
        IsTraceDebug = false
    }

configuration
|> Build
