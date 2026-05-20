using EcoScolarWebApi.Commun;

namespace EcoScolarWebApi.Utils
{
    public class Result<T>
    {
        public bool IsSuccess { get; private set; }
        public T? Data { get; private set; }
        public IEnumerable<string> Errors { get; private set; } = Array.Empty<string>();
        public ErrorType ErrorType { get; private set; }

        /// <summary>
        /// Creates a successful result containing the specified data.
        /// </summary>
        /// <param name="data">The data to include in the successful result. Can be null if the result type allows null values.</param>
        /// <returns>instance representing a successful operation with the provided data.</returns>
        public static Result<T> Success(T data) => new Result<T> { IsSuccess = true, Data = data, ErrorType = ErrorType.None };

        /// <summary>
        /// Creates a failed result containing the specified error messages.
        /// </summary>
        /// <param name="errors">A collection of error messages that describe the reasons for the failure. Cannot be null or empty.</param>
        /// <returns>instance representing a failed operation, with the provided error messages.</returns>
        public static Result<T> Failure(IEnumerable<string> errors, ErrorType errorType = ErrorType.Invalid) => new Result<T> 
        { 
            IsSuccess = false, 
            Errors = errors, 
            ErrorType = errorType 
        };

        /// <summary>
        /// Creates a failed result with the specified error message.
        /// </summary>
        /// <param name="error">The error message that describes the reason for the failure. Cannot be null.</param>
        /// <param name="errorType">The type of error. Defaults to Invalid.</param>
        /// <returns>instance representing a failed operation, containing the provided error message.</returns>
        public static Result<T> Failure(string error, ErrorType errorType = ErrorType.Invalid) => new Result<T> 
        { 
            IsSuccess = false, 
            Errors = new[] { error },
            ErrorType = errorType 
        };
    }

    public class Result
    {
        public bool IsSuccess { get; private set; } 
        public IEnumerable<string> Errors { get; private set; } = Array.Empty<string>();
        public ErrorType ErrorType { get; private set; }

        /// <summary>
        /// Creates a successful result containing the specified data.
        /// </summary>
        /// <returns>instance representing a successful operation</returns>
        public static Result Success() => new Result { IsSuccess = true, ErrorType = ErrorType.None };

        /// <summary>
        /// Creates a failed result with the specified collection of error messages.
        /// </summary>
        /// <param name="errors">A collection of error messages that describe the reasons for the failure. Cannot be null.</param>
        /// <param name="errorType">The type of error. Defaults to Invalid.</param>
        /// <returns>instance representing a failure, containing the provided error messages.</returns>
        public static Result Failure(IEnumerable<string> errors, ErrorType errorType = ErrorType.Invalid) => new Result 
        { 
            IsSuccess = false, 
            Errors = errors, 
            ErrorType = errorType 
        };

        /// <summary>
        /// Creates a failed result with the specified error message.
        /// </summary>
        /// <param name="error">The error message that describes the reason for the failure. Cannot be null or empty.</param>
        /// <param name="errorType">The type of error. Defaults to Invalid.</param>
        /// <returns>instance representing a failure, containing the provided error message.</returns>
        public static Result Failure(string error, ErrorType errorType = ErrorType.Invalid) => new Result 
        { 
            IsSuccess = false, 
            Errors = new[] { error }, 
            ErrorType = errorType 
        };
    }
}
