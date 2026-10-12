namespace Fdw.Services.Etl.Abstractions;

/// <summary>
/// Read-only surface for a single sort field within a Sort transform request (FDW-1109).
/// </summary>
public interface ISortFieldSpec
{
    /// <summary>Gets the field to sort by.</summary>
    string FieldName { get; }

    /// <summary>Gets the direction name (resolved against <c>SortDirections</c>): Ascending or Descending.</summary>
    string Direction { get; }
}
