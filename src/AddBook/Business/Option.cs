using System;

namespace AddBook.Business
{
    internal abstract class Option<T>
    {
        public abstract TResult Match<TResult>(Func<T, TResult> someFunc, Func<TResult> noneFunc);

        public static readonly Option<T> None = new NoneOption();

        public static Option<T> Some(T value) => new SomeOption(value);

        public abstract bool HasSome();

        private sealed class SomeOption : Option<T>
        {
            private readonly T value;

            internal SomeOption(T value)
            {
                this.value = value;
            }

            public override bool HasSome() => true;

            public override TResult Match<TResult>(Func<T, TResult> someFunc, Func<TResult> noneFunc) => someFunc(value);
        }

        private sealed class NoneOption : Option<T>
        {
            internal NoneOption()
            { }

            public override bool HasSome() => false;

            public override TResult Match<TResult>(Func<T, TResult> someFunc, Func<TResult> noneFunc) => noneFunc();
        }
    }
}