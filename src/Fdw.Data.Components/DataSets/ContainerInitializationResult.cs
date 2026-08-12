using System.Collections.Generic;
using Fdw.Services.Data.Clients.Models;

namespace Fdw.Data.Components.DataSets;

/// <summary>
/// The result of initializing a DataSet wizard from an existing DataStore container.
/// Contains a pre-built source row and fields mapped to abstract types.
/// </summary>
public sealed class ContainerInitializationResult
{
    /// <summary>Gets the suggested source name.</summary>
    public string SourceName { get; init; } = "Primary";

    /// <summary>Gets the DataStore name.</summary>
    public string DataStoreName { get; init; } = string.Empty;

    /// <summary>Gets the container name.</summary>
    public string ContainerName { get; init; } = string.Empty;

    /// <summary>Gets the physical path within the DataStore.</summary>
    /// <remarks>
    /// Named PathValue and not Path: a member called Path shadows <see cref="System.IO.Path"/> inside
    /// the declaring type, so <c>Path.Combine(...)</c> there resolves to this value and fails to
    /// compile in a way that reads as nonsense.
    /// </remarks>
    public string PathValue { get; init; } = string.Empty;

    /// <summary>Gets the fields mapped from the container's field list to abstract DataSet types.</summary>
    public IReadOnlyList<CreateDataSetFieldRequest> Fields { get; init; } = [];
}
