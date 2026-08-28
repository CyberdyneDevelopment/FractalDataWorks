using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Results.Abstractions;

/// <summary>
/// Base class for result category implementations using the CRTP pattern.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class ResultCategoryBase : TypeOptionBase<int, ResultCategoryBase>, IResultCategory
{
    /// <summary>
    /// Initializes a new instance for the Empty sentinel.
    /// </summary>
    protected ResultCategoryBase()
        : base(-1, "NotFound")
    {
        IsFailure = true;
        IsRetryable = false;
        HttpStatus = 500;
        ClientMessage = "An unexpected error occurred";
        ClientAction = "Contact your administrator";
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResultCategoryBase"/> class.
    /// </summary>
    protected ResultCategoryBase(int id, string name, bool isFailure, bool isRetryable, int httpStatus, string clientMessage, string? clientAction)
        : base(id, name)
    {
        IsFailure = isFailure;
        IsRetryable = isRetryable;
        HttpStatus = httpStatus;
        ClientMessage = clientMessage;
        ClientAction = clientAction;
    }

    /// <inheritdoc />
    public int RangeBase => Id * 10000;

    /// <inheritdoc />
    public bool IsFailure { get; }

    /// <inheritdoc />
    public bool IsRetryable { get; }

    /// <inheritdoc />
    public int HttpStatus { get; }

    /// <inheritdoc />
    public string ClientMessage { get; }

    /// <inheritdoc />
    public string? ClientAction { get; }
}
