#load "PostGeneration.fsx"

open System
open System.Collections.Generic
open Utils
open BookInfos
open BookConfig
open PostGeneration

let generatePosts = BuildTask
                        "generate posts"
                        (fun _ ->
                            // prerequisite
                            CreateFolderIfNotExists PostsFolder
                            CreateFolderIfNotExists ImagesFolder
                        )
                        (fun configuration ->
                            let bookInfoOption = GetBookInfoFromEnv()
                            match bookInfoOption with
                            | None -> Success configuration
                            | Some (isbn, startDate) ->
                                let booksList = configuration.BooksFilePath |> BookConfig.Load
                                booksList.Add(new BookConfig(isbn, startDate, true))
                                let imagePath, imageContent, postPath, postContent, bookTitle = GeneratePost isbn startDate
                                CommitNewPost bookTitle postPath postContent imagePath imageContent configuration.BooksFilePath (booksList |> BookConfig.ToYaml)
                                WriteNewPost postPath postContent imagePath imageContent
                                Success configuration
                        )

let bakeSite = BuildTask
                "bake site"
                (fun _ ->
                    // prerequisite
                    ExecProcessWithFail "choco" "install pretzel -y"
                )
                (fun configuration ->
                    System.Threading.Thread.CurrentThread.CurrentCulture <- System.Globalization.CultureInfo.CreateSpecificCulture("fr-FR")
                    ExecProcessWithTaskResult
                            @"C:\ProgramData\chocolatey\lib\pretzel\tools\Pretzel.exe"
                            "bake site"
                            configuration
                            "Pretzel have failed to build the site"
                )

let deploySite = BuildTask
                    "deploy"
                    (fun _ ->
                        // prerequisite
                        System.Environment.SetEnvironmentVariable("PATH", ("C:\\Python35;C:\\Python35\\Scripts;" + Environment.GetEnvironmentVariable "PATH"))
                        ExecProcessWithFail "pip" "install creep"
                    )
                    (fun configuration ->
                        ExecProcessWithTaskResult
                            "creep"
                            (["""-e "{""default"": {""connection"": ""ftp://"""; configuration.FtpUser; ":"; configuration.FtpPassword; """@laedit.net""}}" -d "{""source"": ""hash""}" -b site/_site -y"""] |> Seq.fold (+) "")
                            configuration
                            "Creep have failed to deploy the site"
                    )

let log result =
    match result with
    | Success s -> ()
    | Failure (c, f) -> Warning f

let Build =
    startBuild generatePosts
    >> executeTask bakeSite
    >> executeTask deploySite
    >> log

{
    BooksFilePath = "books.yml";
    FtpUser = Environment.GetEnvironmentVariable("ftp_user");
    FtpPassword = Environment.GetEnvironmentVariable("ftp_password");
    IsTraceDebug = false
}
|> Build
