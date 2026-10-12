using Fdw.Collections;

namespace Fdw.Results.Abstractions;

/// <summary>
/// Interface for result status levels indicating outcome nuance.
/// </summary>
public interface IResultStatus : ITypeOption<int, ResultStatusBase>
{
    /// <summary>
    /// Gets whether this status represents a success-class outcome.
    /// </summary>
    bool IsSuccess { get; }

    /// <summary>
    /// Gets whether this status indicates the caller should inspect messages.
    /// </summary>
    bool RequiresAttention { get; }
}
