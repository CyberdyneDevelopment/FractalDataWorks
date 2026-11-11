using Fdw.Messages;

namespace Fdw.Data.RowSources.Abstractions;

/// <summary>
/// Optional reader capability: a reader that ends its read because its input is malformed says why, rather than throwing through the
/// cursor or stopping as if the input had ended (FDW-4971).
/// </summary>
/// <remarks>
/// A reader returns false from <c>Read</c> and sets <see cref="ReadFailure"/>; <see cref="CursorRecordSource"/> then hands out that failure
/// as its last element, after every record read before it.
/// </remarks>
public interface IReportsReadFailure
{
    /// <summary>Gets why the read ended early, or null while it has not.</summary>
    IGenericMessage? ReadFailure { get; }
}
