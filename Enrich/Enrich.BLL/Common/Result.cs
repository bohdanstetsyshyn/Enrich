namespace Enrich.BLL.Common
{
    public class Result
    {
        protected Result(bool isSuccess, string? errorMessage)
        {
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage;
        }

        public bool IsSuccess { get; }

        public string? ErrorMessage { get; }

        public static Result Success() => new Result(true, null);

        public static Result Failure(string errorMessage) => new Result(false, errorMessage);
    }
}
