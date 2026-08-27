using System;
using Fdw.Configuration;

namespace Fdw.Services.Data.Abstractions;

/// <summary>
/// Marker interface for typed data store body configurations
/// (MsSqlDataStoreConfiguration, FileSystemDataStoreConfiguration, etc.).
/// Each typed body implements this interface directly without inheriting from
/// <c>DataStoreConfiguration</c>.
/// </summary>
/// <remarks>
/// DataStore bodies are persisted in their own tables (data.MsSqlDataStore,
/// data.FileSystemDataStore, etc.) and linked to the parent <c>data.DataStore</c>
/// row via a <c>DataStoreId</c> foreign key property.
/// The parent <c>DataStoreConfiguration</c> carries an
/// <c>IDataStoreConfiguration? Configuration</c> property populated on the read path.
/// </remarks>
public interface IDataStoreConfiguration : IImplementationConfiguration
{
    /// <summary>Gets or sets the FK to the parent DataStore's logical Id.</summary>
    Guid DataStoreId { get; set; }
}
