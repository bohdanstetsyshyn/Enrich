namespace Enrich.BLL.Common
{
    public sealed class Result<T> : Result
    {
        private Result(bool isSuccess, T? value, string? errorMessage)
            : base(isSuccess, errorMessage)
        {
            Value = value;
        }

        public T? Value { get; }

#pragma warning disable CA1000 // Factory methods on generic types require type argument at call site
        public static Result<T> Success(T value) => new Result<T>(true, value, null);

        public static new Result<T> Failure(string errorMessage) => new Result<T>(false, default, errorMessage);
#pragma warning restore CA1000
    }
}
