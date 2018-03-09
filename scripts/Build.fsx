#load "PostGeneration.fsx"
#load "GitHelper.fsx"

open System
open Utils
open BookConfig
open PostGeneration
open GitHelper

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
                                let existingBookIndex = booksList.FindIndex (fun b -> b.Isbn = isbn)
                                booksList.Add(new BookConfig(isbn, startDate, true))

                                match existingBookIndex with
                                | -1 -> let imagePath, imageContent, postPath, postContent, bookTitle = GeneratePost isbn startDate
                                        GitHelper.Commit [{ Path = postPath; Content = Text postContent };
                                                          { Path = imagePath; Content = Image imageContent };
                                                          { Path = configuration.BooksFilePath; Content = Text (booksList |> BookConfig.ToYaml) }]
                                                         (sprintf "Add new book '%s' [skip ci]" bookTitle)
                                        WriteNewPost postPath postContent imagePath imageContent
                                | index -> let originalDate = booksList.Item(index).Date;
                                           let postPath, postContent, bookTitle = GetPostInfo isbn originalDate startDate;
                                           GitHelper.Commit [{ Path = postPath; Content = Text postContent};
                                                             { Path = configuration.BooksFilePath; Content = Text (booksList |> BookConfig.ToYaml) }]
                                                             (sprintf "Duplicate book '%s' [skip ci]" bookTitle)
                                           WriteTextFile postPath postContent

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
    | Success _ -> ()
    | Failure (_, f) -> Warning f

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
