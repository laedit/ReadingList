﻿using AddBook.Business.Database;
using AddBook.Business.GitHub;
using System;
using System.Text;
using System.Threading.Tasks;

namespace AddBook.Business.Search.Book
{
    internal sealed class PostsBookSearch : IBookSearch
    {
        private readonly BooksRepository booksRepository;
        private readonly GitHubHelper gitHubHelper;

        public string Name => "Posts";

        public PostsBookSearch(BooksRepository booksRepository, GitHubHelper gitHubHelper)
        {
            this.booksRepository = booksRepository;
            this.gitHubHelper = gitHubHelper;
        }

        public async Task<Result<Book>> Search(string isbn)
        {
            // Vérifier depuis books.yml et récupérer les infos depuis le post
            return await (await booksRepository.GetBooks()).Find(isbn).Match(async bi =>
            {
                var searchResult = await gitHubHelper.SearchPost(bi.Date);

                return await searchResult.Match(async postPathes =>
                {
                    foreach (var postPath in postPathes)
                    {
                        var goodBook = false;
                        var book = new Book { Isbn = isbn };

                        var postContent = (await gitHubHelper.GetFileContent(postPath)).Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                        // Parsing post content
                        var summary = new StringBuilder();
                        foreach (var postLine in postContent)
                        {
                            if (postLine.StartsWith("title:"))
                            {
                                book.Title = postLine[8..].Trim('"');
                                continue;
                            }
                            if (postLine.StartsWith("author:"))
                            {
                                book.Author = postLine[9..].Trim('"');
                                continue;
                            }
                            if (postLine.StartsWith("editor:"))
                            {
                                book.Editor = postLine[9..].Trim('"');
                                continue;
                            }

                            if (postLine.StartsWith("![Couverture]"))
                            {
                                book.CoverUrl = postLine[14..postLine.IndexOf(')')];
                                summary.AppendLine(postLine[(postLine.IndexOf(')') + 1)..]);
                                continue;
                            }

                            if (postLine.StartsWith("---") || postLine.StartsWith("layout:"))
                            {
                                continue;
                            }

                            if (postLine.StartsWith("isbn:"))
                            {
                                if (postLine[7..].Trim('"') == isbn)
                                {
                                    goodBook = true;
                                    continue;
                                }
                                break;
                            }

                            summary.AppendLine(postLine);
                        }

                        if (goodBook)
                        {
                            book.Summary = summary.ToString().Trim();
                            return Result<Book>.Success(book);
                        }
                    }
                    return Result<Book>.Fail($"Post not found in repository amon pathes:{string.Join(Environment.NewLine, postPathes)}");

                }, () => Task.FromResult(Result<Book>.Fail($"Post not found in repository with date '{bi.Date}'.")));
            }, () => Task.FromResult(Result<Book>.Fail("Not in the base yet.")));
        }
    }
}