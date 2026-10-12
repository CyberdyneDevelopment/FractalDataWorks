using Fdw.Collections;

namespace Fdw.Services.Quality.Abstractions.TypeCollections.PromotionStatusTypeOptions;

/// <summary>
/// Represents a status for promotion workflow tracking.
/// </summary>
public interface IPromotionStatusType : ITypeOption<int, PromotionStatusTypeBase>
{
    /// <summary>
    /// Gets a value indicating whether this status allows further state transitions.
    /// </summary>
    bool IsTerminal { get; }

    /// <summary>
    /// Gets a value indicating whether this status indicates success.
    /// </summary>
    bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether this status allows execution.
    /// </summary>
    bool AllowsExecution { get; }
}
