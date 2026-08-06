namespace Fdw.Data.Abstractions;

/// <summary>
/// Interface for paging expressions (SKIP / TAKE representation).
/// </summary>
/// <remarks>
/// Represents pagination parameters.
/// Translators convert this to SQL OFFSET/FETCH, OData $skip/$top, etc.
/// </remarks>
public interface IPagingExpression
{
    /// <summary>
    /// Gets the number of records to skip.
    /// </summary>
    /// <value>The offset. 0 means start from the beginning.</value>
    int Skip { get; }

    /// <summary>
    /// Gets the maximum number of records to return.
    /// </summary>
    /// <value>The limit. Null means no limit.</value>
    int? Take { get; }
}
