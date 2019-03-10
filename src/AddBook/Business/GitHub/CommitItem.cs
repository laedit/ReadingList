using System;

namespace AddBook.Business.GitHub
{
    internal abstract class CommitItem
    {
        protected readonly string path;

        private CommitItem(string path) => this.path = path;

        public abstract TResult Match<TResult>(Func<string, string, TResult> textFunc, Func<string, byte[], TResult> imageFunc);

        public static CommitItem Create(string path, string content) => new TextCommitItem(path, content);
        public static CommitItem Create(string path, byte[] content) => new ImageCommitItem(path, content);

        private sealed class TextCommitItem : CommitItem
        {
            private readonly string content;

            public TextCommitItem(string path, string content)
                : base(path)
            {
                this.content = content;
            }

            public override TResult Match<TResult>(Func<string, string, TResult> textFunc, Func<string, byte[], TResult> imageFunc) => textFunc(path, content);
        }

        private sealed class ImageCommitItem : CommitItem
        {
            private readonly byte[] content;

            public ImageCommitItem(string path, byte[] content)
                : base(path)
            {
                this.content = content;
            }

            public override TResult Match<TResult>(Func<string, string, TResult> textFunc, Func<string, byte[], TResult> imageFunc) => imageFunc(path, content);
        }
    }
}