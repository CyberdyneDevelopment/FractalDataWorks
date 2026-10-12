using System;

namespace Fdw.Web.Clients.Abstractions;

/// <summary>
/// Result type for API operations that captures success/failure with context.
/// </summary>
/// <typeparam name="T">The success value type.</typeparam>
public sealed class ApiResult<T>
{
    /// <summary>
    /// Gets whether the operation succeeded.
    /// </summary>
    public bool IsSuccess { get; private init; }

    /// <summary>
    /// Gets the success value (only valid when IsSuccess is true).
    /// </summary>
    public T? Value { get; private init; }

    /// <summary>
    /// Gets the error details (only valid when IsSuccess is false).
    /// </summary>
    public ApiError? Error { get; private init; }

    private ApiResult() { }

    /// <summary>
    /// Creates a successful result with the given value.
    /// </summary>
    /// <param name="value">The success value.</param>
    /// <returns>A successful result.</returns>
    public static ApiResult<T> Success(T value) => new()
    {
        IsSuccess = true,
        Value = value
    };

    /// <summary>
    /// Creates a failed result with the given error.
    /// </summary>
    /// <param name="error">The error details.</param>
    /// <returns>A failed result.</returns>
    public static ApiResult<T> Failure(ApiError error) => new()
    {
        IsSuccess = false,
        Error = error
    };

    /// <summary>
    /// Creates a failed result with a simple title and detail.
    /// </summary>
    /// <param name="title">The error title.</param>
    /// <param name="detail">The error detail.</param>
    /// <param name="status">The HTTP status code.</param>
    /// <returns>A failed result.</returns>
    public static ApiResult<T> Failure(string title, string detail, int status = 400) => new()
    {
        IsSuccess = false,
        Error = new ApiError
        {
            Type = status switch
            {
                400 => ApiError.Types.Validation,
                404 => ApiError.Types.NotFound,
                409 => ApiError.Types.Conflict,
                401 => ApiError.Types.Unauthorized,
                403 => ApiError.Types.Forbidden,
                429 => ApiError.Types.RateLimited,
                _ => ApiError.Types.ServerError
            },
            Title = title,
            Detail = detail,
            Status = status
        }
    };

    /// <summary>
    /// Maps the success value to a new type.
    /// </summary>
    /// <typeparam name="TNew">The new value type.</typeparam>
    /// <param name="mapper">The mapping function.</param>
    /// <returns>A new result with the mapped value or the original error.</returns>
    public ApiResult<TNew> Map<TNew>(Func<T, TNew> mapper)
    {
        return IsSuccess
            ? ApiResult<TNew>.Success(mapper(Value!))
            : ApiResult<TNew>.Failure(Error!);
    }

    /// <summary>
    /// Executes an action on success, returning self for chaining.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <returns>This result for chaining.</returns>
    public ApiResult<T> OnSuccess(Action<T> action)
    {
        if (IsSuccess)
        {
            action(Value!);
        }

        return this;
    }

    /// <summary>
    /// Executes an action on failure, returning self for chaining.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <returns>This result for chaining.</returns>
    public ApiResult<T> OnFailure(Action<ApiError> action)
    {
        if (!IsSuccess)
        {
            action(Error!);
        }

        return this;
    }

    /// <summary>
    /// Gets the value or a default if the result is a failure.
    /// </summary>
    /// <param name="defaultValue">The default value.</param>
    /// <returns>The value or default.</returns>
    public T? GetValueOrDefault(T? defaultValue = default)
    {
        return IsSuccess ? Value : defaultValue;
    }
}

/// <summary>
/// Non-generic result for void operations.
/// </summary>
public sealed class ApiResult
{
    /// <summary>
    /// Gets whether the operation succeeded.
    /// </summary>
    public bool IsSuccess { get; private init; }

    /// <summary>
    /// Gets the error details (only valid when IsSuccess is false).
    /// </summary>
    public ApiError? Error { get; private init; }

    private ApiResult() { }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <returns>A successful result.</returns>
    public static ApiResult Success() => new() { IsSuccess = true };

    /// <summary>
    /// Creates a failed result with the given error.
    /// </summary>
    /// <param name="error">The error details.</param>
    /// <returns>A failed result.</returns>
    public static ApiResult Failure(ApiError error) => new() { IsSuccess = false, Error = error };

    /// <summary>
    /// Creates a failed result with a simple title and detail.
    /// </summary>
    /// <param name="title">The error title.</param>
    /// <param name="detail">The error detail.</param>
    /// <param name="status">The HTTP status code.</param>
    /// <returns>A failed result.</returns>
    public static ApiResult Failure(string title, string detail, int status = 400)
    {
        return new ApiResult
        {
            IsSuccess = false,
            Error = new ApiError
            {
                Type = status switch
                {
                    400 => ApiError.Types.Validation,
                    404 => ApiError.Types.NotFound,
                    409 => ApiError.Types.Conflict,
                    401 => ApiError.Types.Unauthorized,
                    403 => ApiError.Types.Forbidden,
                    429 => ApiError.Types.RateLimited,
                    _ => ApiError.Types.ServerError
                },
                Title = title,
                Detail = detail,
                Status = status
            }
        };
    }

    /// <summary>
    /// Executes an action on failure, returning self for chaining.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <returns>This result for chaining.</returns>
    public ApiResult OnFailure(Action<ApiError> action)
    {
        if (!IsSuccess)
        {
            action(Error!);
        }

        return this;
    }
}
