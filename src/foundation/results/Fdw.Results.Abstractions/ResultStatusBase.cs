using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Results.Abstractions;

/// <summary>
/// Base class for result status implementations using the CRTP pattern.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class ResultStatusBase : TypeOptionBase<int, ResultStatusBase>, IResultStatus
{
    /// <summary>
    /// Initializes a new instance for the Empty sentinel.
    /// </summary>
    protected ResultStatusBase()
        : base(-1, "NotFound")
    {
        IsSuccess = false;
        RequiresAttention = true;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResultStatusBase"/> class.
    /// </summary>
    protected ResultStatusBase(
        int id,
        string name,
        bool isSuccess,
        bool requiresAttention)
        : base(id, name)
    {
        IsSuccess = isSuccess;
        RequiresAttention = requiresAttention;
    }

    /// <inheritdoc />
    public bool IsSuccess { get; }

    /// <inheritdoc />
    public bool RequiresAttention { get; }
}
