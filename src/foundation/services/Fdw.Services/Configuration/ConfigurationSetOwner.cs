namespace Fdw.Services.Configuration;

/// <summary>
/// One owner row of a set read: the record, and the physical key its child rows hang from.
/// </summary>
/// <param name="Row">The detached record the children are set on.</param>
/// <param name="RowId">The owner row's physical key, as the store holds it; a child row's Foreign key holds this value.</param>
internal sealed record ConfigurationSetOwner(object Row, long RowId);
