using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Results;
using Fdw.Data.Abstractions;

namespace Fdw.Data.DataStores.Abstractions;

/// <summary>
/// Represents a navigable path within a data store to locate specific data.
/// DataPaths abstract the navigation structure (folders, endpoints, schemas) 
/// from the physical storage mechanism.
/// </summary>
/// <remarks>
/// IDataNodePath represents HOW to navigate within a data store to find specific data.
/// It's separate from the store location and the data format. Examples:
/// - SQL: Database.Schema.Table (e.g., "SalesDB.dbo.Orders")
/// - REST: Endpoint segments (e.g., "/api/v1/customers/{id}/orders")
/// - File: Directory/filename pattern (e.g., "data/2024/weather_seattle.csv")
/// - GraphQL: Query path (e.g., "user.profile.orders")
/// </remarks>
public interface IDataNodePath
{
    /// <summary>
    /// Gets the unique identifier for this data path.
    /// </summary>
    /// <value>A unique identifier within the data store.</value>
    string Id { get; }

    /// <summary>
    /// Gets the display name for this data path.
    /// </summary>
    /// <value>A human-readable name for UI and logging purposes.</value>
    string Name { get; }

    /// <summary>
    /// Gets the path type (e.g., "SqlTable", "RestEndpoint", "FilePattern").
    /// </summary>
    /// <value>The path type identifier for navigation logic.</value>
    string PathType { get; }

    /// <summary>
    /// Gets the full path string in the format appropriate for the store type.
    /// </summary>
    /// <value>
    /// The complete path specification:
    /// - SQL: "Database.Schema.Table" or "Schema.Table"
    /// - HTTP: "/api/v1/resource/{id}" or "resource/subresource"
    /// - File: "folder/subfolder/pattern.ext" or "data/*.csv"
    /// - GraphQL: "query.field.subfield"
    /// </value>
    string FullPath { get; }

    /// <summary>
    /// Gets the segments that make up this path.
    /// </summary>
    /// <value>
    /// The individual components of the path for programmatic navigation.
    /// For example, "/api/v1/users/{id}" becomes ["api", "v1", "users", "{id}"].
    /// </value>
    IReadOnlyList<string> Segments { get; }

    /// <summary>
    /// Gets parameters that can be substituted in this path.
    /// </summary>
    /// <value>
    /// Parameter definitions for dynamic path segments like {id}, {date}, etc.
    /// Keys are parameter names, values are parameter metadata.
    /// </value>
    IReadOnlyDictionary<string, PathParameter> Parameters { get; }

    /// <summary>
    /// Gets metadata about this data path.
    /// </summary>
    /// <value>Additional properties and configuration information.</value>
    IReadOnlyDictionary<string, object> Metadata { get; }

    /// <summary>
    /// Gets a value indicating whether this path requires parameters to be resolved.
    /// </summary>
    /// <value>
    /// <c>true</c> if the path contains parameter placeholders that must be
    /// substituted before use; otherwise, <c>false</c>.
    /// </value>
    bool RequiresParameters { get; }

    /// <summary>
    /// Resolves parameter placeholders in the path with actual values.
    /// </summary>
    /// <param name="parameters">The parameter values to substitute.</param>
    /// <returns>A new path with parameters resolved, or the same path if no parameters.</returns>
    /// <remarks>
    /// This method creates a concrete path by substituting parameter placeholders
    /// with actual values. For example, "/api/users/{id}" with {"id": "123"} 
    /// becomes "/api/users/123".
    /// </remarks>
    IDataNodePath ResolveParameters(IDictionary<string, object> parameters);

    /// <summary>
    /// Validates whether the provided parameters satisfy this path's requirements.
    /// </summary>
    /// <param name="parameters">The parameter values to validate.</param>
    /// <returns>
    /// A result indicating whether the parameters are valid, with specific
    /// error messages for any validation failures.
    /// </returns>
    IGenericResult ValidateParameters(IDictionary<string, object> parameters);

    /// <summary>
    /// Gets the parent path if this path is hierarchical.
    /// </summary>
    /// <returns>The parent path, or null if this is a root path.</returns>
    /// <remarks>
    /// This method supports hierarchical path navigation. For example,
    /// the parent of "Database.Schema.Table" would be "Database.Schema".
    /// </remarks>
    IDataNodePath? GetParent();

    /// <summary>
    /// Gets child paths if this path supports hierarchical navigation.
    /// </summary>
    /// <returns>Child paths accessible from this path.</returns>
    /// <remarks>
    /// This method supports discovery of sub-paths. For example, a database
    /// schema path might return table paths as children.
    /// </remarks>
    IEnumerable<IDataNodePath> GetChildren();

    /// <summary>
    /// Combines this path with a relative path to create a new path.
    /// </summary>
    /// <param name="relativePath">The relative path to append.</param>
    /// <returns>A new path combining this path with the relative path.</returns>
    /// <remarks>
    /// This method supports path composition. For example, combining
    /// "/api/users" with "{id}/orders" creates "/api/users/{id}/orders".
    /// </remarks>
    IDataNodePath Combine(string relativePath);
}

/// <summary>
/// Generic data path interface that holds typed containers.
/// </summary>
/// <typeparam name="TContainer">The type of containers this path holds.</typeparam>
/// <remarks>
/// Why (Stage 3 — connection-composition): this generic was tentatively deleted in Stage 1 as
/// "unused", but it is still load-bearing for the PHYSICAL-address path types
/// (<c>DatabasePath</c>/<c>HttpPath</c>/<c>PostgreSqlDatabasePath</c>/<c>FilePath</c>) and the
/// legacy <c>Data.Builders</c> fluent family. The foundational redesign defers the physical-path
/// rework to a later stage, so the interface is restored here to keep those packages building
/// without dragging the legacy path rework into the builder/rewire stage. It carries the
/// container-on-path projection that the physical paths still expose; the uniform
/// <see cref="Fdw.Data.Abstractions.IDataNodePath"/> tree-navigation node (with
/// <c>Containers</c>/<c>Container(name)</c>) is the model going forward.
/// </remarks>
public interface IDataNodePath<TContainer> : IDataNodePath
    where TContainer : IStorageContainer
{
    /// <summary>
    /// Gets the containers available at this path.
    /// </summary>
    IReadOnlyList<TContainer> Containers { get; }

    /// <summary>
    /// Gets a specific container by name.
    /// </summary>
    /// <param name="name">The container name to find.</param>
    /// <returns>The container if found; otherwise, null.</returns>
    TContainer? GetContainer(string name);

    /// <summary>
    /// Checks if a container with the specified name exists in this path.
    /// </summary>
    /// <param name="name">The container name to check.</param>
    /// <returns>true if the container exists; otherwise, false.</returns>
    bool ContainsContainer(string name);
}
