using System.Collections.Generic;

namespace Fdw.Services.Etl.Abstractions;

/// <summary>
/// Read-only surface for the lookup configuration within a Lookup transform request.
/// </summary>
public interface ILookupSpec
{
    /// <summary>Gets the lookup data set name, or empty when the lookup reads a data store container.</summary>
    string LookupDataSet { get; }

    /// <summary>Gets the data store the lookup reads, when it reads a container rather than a data set (FDW-1269); all three parts or none.</summary>
    string? LookupDataStoreName { get; }

    /// <summary>Gets the path within the lookup data store.</summary>
    string? LookupDataStorePath { get; }

    /// <summary>Gets the container within that path whose rows the lookup reads.</summary>
    string? LookupContainerName { get; }

    /// <summary>Gets the lookup key field (in the lookup source).</summary>
    string LookupKeyField { get; }

    /// <summary>Gets the source key field to match against.</summary>
    string SourceKeyField { get; }

    /// <summary>Gets the optional output field prefix.</summary>
    string? OutputFieldPrefix { get; }

    /// <summary>Gets the lookup columns to bring across — one output field per column.</summary>
    IReadOnlyList<string> LookupColumns { get; }

    /// <summary>Gets the join type name (resolved against <c>LookupJoinTypes</c>).</summary>
    string JoinType { get; }
}
