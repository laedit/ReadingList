using System;

namespace AddBook.Business
{
    public abstract class Result<T>
    {
        public abstract TResult Match<TResult>(Func<T, TResult> successFunc, Func<string, TResult> failFunc);

        public static Result<T> Fail(string reason) => new FailResult(reason);

        public static Result<T> Success(T value) => new SuccessResult(value);
        
        private sealed class SuccessResult : Result<T>
        {
            private readonly T value;

            public SuccessResult(T value)
            {
                this.value = value;
            }
            
            public override TResult Match<TResult>(Func<T, TResult> successFunc, Func<string, TResult> failFunc) => successFunc(value);
        }

        private sealed class FailResult : Result<T>
        {
            private readonly string reason;

            public FailResult(string reason)
            {
                this.reason = reason;
            }
            
            public override TResult Match<TResult>(Func<T, TResult> successFunc, Func<string, TResult> failFunc) => failFunc(reason);
        }
    }
}