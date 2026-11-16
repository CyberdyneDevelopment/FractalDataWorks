using Fdw.Services.Data.Abstractions.DataOptionRows;
using Fdw.Services.Data.Clients.Models;

namespace Fdw.Services.Data.Endpoints;

/// <summary>Maps an option row to the payload the option-row routes answer (FDW-5664).</summary>
internal static class DataOptionRowMapping
{
    /// <summary>Maps a row.</summary>
    /// <param name="row">The row.</param>
    /// <returns>Its payload.</returns>
    internal static DataOptionRowPayload ToPayload(DataOptionRow row) => new()
    {
        Id = row.Id,
        Column = row.Column,
        Value = row.Value,
        OptionType = row.OptionType,
        Option = row.Option,
        Active = row.Active,
    };

    /// <summary>The container a request names in its route.</summary>
    /// <param name="dataStore">The data store.</param>
    /// <param name="path">The path.</param>
    /// <param name="container">The container.</param>
    /// <returns>The container.</returns>
    internal static DataOptionRowsContainer Target(string dataStore, string path, string container)
        => new() { DataStore = dataStore, Path = path, Container = container };
}
