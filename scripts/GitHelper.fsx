#r "../libs/Octokit.dll"

open System
open Octokit

type CommitItemContent = Text of string | Image of byte[]

type CommitItem =
    {
        Path: string;
        Content: CommitItemContent
    }

/// Create image tree item
let private CreateImageTreeItem imagePath imageContent (githubClient:IGitHubClient) owner repo =
    let newImageBlob = new Octokit.NewBlob()
    newImageBlob.Encoding <- EncodingType.Base64
    newImageBlob.Content <- Convert.ToBase64String(imageContent)
    let imageBlob = githubClient.Git.Blob.Create(owner, repo, newImageBlob).Result

    let imageTreeItem = new NewTreeItem()
    imageTreeItem.Mode <- "100644" // new file
    imageTreeItem.Type <- TreeType.Blob
    imageTreeItem.Path <- imagePath
    imageTreeItem.Sha <- imageBlob.Sha
    imageTreeItem

/// Create text tree item
let private CreateTextTreeItem textPath textContent =
    let textTreeItem = new NewTreeItem()
    textTreeItem.Mode <- "100644"
    textTreeItem.Type <- TreeType.Blob
    textTreeItem.Path <- textPath
    textTreeItem.Content <- textContent
    textTreeItem

/// Create tree item
let private CreateTreeItem githubClient owner repo treeItem =
    match treeItem.Content with
    | Image imageContent -> CreateImageTreeItem treeItem.Path imageContent githubClient owner repo
    | Text textContent -> CreateTextTreeItem treeItem.Path textContent

/// Create a commit and push it to the branch
let Commit commitItems message =

    let owner = "laedit"
    let repo = "readinglist"
    let headMasterRef = "heads/master"

    let githubClient = new Octokit.GitHubClient(new ProductHeaderValue("Laedit-ReadingList"))
    githubClient.Credentials <- new Credentials(Environment.GetEnvironmentVariable("access_token"));

    let master = githubClient.Git.Reference.Get(owner, repo, headMasterRef).Result
    let baseTree = githubClient.Git.Commit.Get(owner, repo, master.Object.Sha).Result

    let nt = new NewTree()
    nt.BaseTree <- baseTree.Sha

    commitItems
    |> List.map (CreateTreeItem githubClient owner repo)
    |> List.iter nt.Tree.Add

    let newTree = githubClient.Git.Tree.Create(owner, repo, nt).Result
    let newCommit = new NewCommit(message, newTree.Sha, master.Object.Sha)

    let commit = githubClient.Git.Commit.Create(owner, repo, newCommit).Result

    githubClient.Git.Reference.Update(owner, repo, headMasterRef, new ReferenceUpdate(commit.Sha)).Result
    |> ignore
