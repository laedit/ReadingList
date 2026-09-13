#!/usr/bin/env -S dotnet --
// allows to execute the file directly
// docs - https://learn.microsoft.com/en-us/dotnet/core/sdk/file-based-apps
using System.Text;

const string YearIndexTemplate = """
+++
sort_by = "date"
transparent = true
render = false
+++

""";

const string MonthIndexTemplate = """
+++
sort_by = "date"
transparent = true
render = false
+++

""";

const string DayIndexTemplate = """
+++
sort_by = "date"
transparent = true
render = false
+++

""";

Console.WriteLine("Converter from Pretzel to Zola");
var rootDir = GetRootDir();
Console.WriteLine($"Root directory: {rootDir}");

var postsDirPath = $"{rootDir}/site/_posts";

Directory.Delete($"{rootDir}/src/content/", true);

foreach (var postPath in Directory.GetFiles(postsDirPath))
{
    Console.WriteLine($"Processing {postPath}");
    // parsing of file path to properties
    var postInfo = ParsePath(postPath);

    var yearDirPath = $"{rootDir}/src/content/{postInfo.Year}";
    if (!Directory.Exists(yearDirPath))
    {
        Directory.CreateDirectory(yearDirPath);
        await File.WriteAllTextAsync($"{yearDirPath}/_index.md", YearIndexTemplate);
    }

    var monthDirPath = $"{yearDirPath}/{postInfo.Month}";
    if (!Directory.Exists(monthDirPath))
    {
        Directory.CreateDirectory(monthDirPath);
        await File.WriteAllTextAsync($"{monthDirPath}/_index.md", MonthIndexTemplate);
    }

    var dayDirPath = $"{monthDirPath}/{postInfo.Day}";
    if (!Directory.Exists(dayDirPath))
    {
        Directory.CreateDirectory(dayDirPath);
        await File.WriteAllTextAsync($"{dayDirPath}/_index.md", DayIndexTemplate);
    }

    await File.WriteAllTextAsync($"{dayDirPath}/{postInfo.Filename}", await ConvertPostAsync(postInfo));
}

var coversDirPath = $"{rootDir}/site/img";
var newCoversDirPath = $"{rootDir}/src/static/img";

foreach (var coverPath in Directory.GetFiles(coversDirPath))
{
    var newCoverPath = Path.Combine(newCoversDirPath, Path.GetFileName(coverPath));
    if(!coverPath.EndsWith("favicon.ico")
        && ! File.Exists(newCoverPath))
    {
        File.Copy(coverPath, newCoverPath);
    }
}

static string GetRootDir()
{
    var currentDir = Environment.CurrentDirectory;
    while (!currentDir.EndsWith("ReadingList"))
    {
        currentDir = Directory.GetParent(currentDir)!.FullName;
    }
    return currentDir;
}

static PostInfo ParsePath(string path)
{
    var fileName = Path.GetFileName(path);
    var pathParts = fileName.Split('-');
    return new(pathParts[0], pathParts[1], pathParts[2], string.Join("-", pathParts.Skip(3)), path);
}

static PostContent ParseYamlPost(string[] postContent)
{
    var content = new StringBuilder();
    var headers = new PostHeaders();

    var inFrontMatter = false;
    foreach (var line in postContent)
    {
        if (line == "---")
        {
            inFrontMatter = !inFrontMatter;
            continue;
        }

        if (inFrontMatter)
        {
            var separatorIndex = line.IndexOf(':');
            var key = line[..separatorIndex].Trim();
            var value = line[(separatorIndex + 1)..].Trim();
            if (!value.StartsWith('"'))
            {
                value = $"\"{value}\"";
            }

            switch (key)
            {
                case "date" : headers = headers with { Date = value }; break;
                case "layout" : headers = headers with { Kind = value }; break;
                case "author" : headers = headers with { Author = value }; break;
                case "editor" : headers = headers with { Editor = value }; break;
                case "isbn" : headers = headers with { Isbn = value }; break;
                case "title" : headers = headers with { Title = value }; break;
                // TODO add magazine name and not just issue name?
                // en faire une taxonomie pour toutes les séries y compris celles de livre / mangas / bds / comics ?
                // => permettrait de retrouver facilement lesquels déjà lu dans une série
                // à voir si utile et simple à récupérer pour les livres
            }
            continue;
        }
        content.AppendLine(line);
    }
    return new PostContent(headers, content.ToString());
}

static string ConvertToTomlPost(PostInfo postInfo, PostContent postContent)
{
    var convertedPostContent = new StringBuilder();
    convertedPostContent.AppendLine("+++");
    convertedPostContent.AppendLine($"template = \"reading-details.html\"");
    convertedPostContent.AppendLine($"title = {postContent.Headers.Title}");
    if (!string.IsNullOrEmpty(postContent.Headers.Date))
    {
        convertedPostContent.AppendLine($"date = {postContent.Headers.Date}");
    }
    else
    {
        convertedPostContent.AppendLine($"date = \"{postInfo.Year}-{postInfo.Month}-{postInfo.Day}\"");
    }
    convertedPostContent.AppendLine($"aliases = [\"{postInfo.Year}/{postInfo.Month}/{postInfo.Day}/{Path.GetFileNameWithoutExtension(postInfo.Filename)}.html\"]");

    convertedPostContent.AppendLine("[extra]");
    convertedPostContent.AppendLine($"kind = {postContent.Headers.Kind}");
    if(!string.IsNullOrEmpty(postContent.Headers.Author))
    {
        convertedPostContent.AppendLine($"author = {postContent.Headers.Author}");
    }
    if (!string.IsNullOrEmpty(postContent.Headers.Isbn))
    {
        convertedPostContent.AppendLine($"isbn = {postContent.Headers.Isbn}");
    }
    if (!string.IsNullOrEmpty(postContent.Headers.Editor))
    {
        convertedPostContent.AppendLine($"editor = {postContent.Headers.Editor}");
    }
    convertedPostContent.AppendLine("+++");

    convertedPostContent.Append(postContent.Content);

    return convertedPostContent.ToString();
}

static async Task<string> ConvertPostAsync(PostInfo postInfo)
{
    var postContent = ParseYamlPost(await File.ReadAllLinesAsync(postInfo.FilePath));

    return ConvertToTomlPost(postInfo, postContent);
}

record PostInfo(string Year, string Month, string Day, string Filename, string FilePath);
record PostHeaders(string? Date = default, string? Title = default, string? Author = default, string? Editor = default, string? Isbn = default, string? Kind = default);
record PostContent(PostHeaders Headers, string Content);
