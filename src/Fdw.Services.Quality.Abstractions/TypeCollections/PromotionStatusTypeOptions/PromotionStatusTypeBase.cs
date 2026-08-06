using Fdw.Collections;

namespace Fdw.Services.Quality.Abstractions.TypeCollections.PromotionStatusTypeOptions;

/// <summary>
/// Base class for promotion status types using the CRTP pattern.
/// </summary>
public abstract class PromotionStatusTypeBase : TypeOptionBase<int, PromotionStatusTypeBase>, IPromotionStatusType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PromotionStatusTypeBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier.</param>
    /// <param name="name">The unique name.</param>
    /// <param name="isTerminal">Whether this status allows further state transitions.</param>
    /// <param name="isSuccess">Whether this status indicates success.</param>
    /// <param name="allowsExecution">Whether this status allows execution.</param>
    protected PromotionStatusTypeBase(
        int id,
        string name,
        bool isTerminal,
        bool isSuccess,
        bool allowsExecution)
        : base(id, name)
    {
        IsTerminal = isTerminal;
        IsSuccess = isSuccess;
        AllowsExecution = allowsExecution;
    }

    /// <inheritdoc/>
    public bool IsTerminal { get; }

    /// <inheritdoc/>
    public bool IsSuccess { get; }

    /// <inheritdoc/>
    public bool AllowsExecution { get; }
}
