using System;
using System.Collections.Generic;
using Fdw.Commands.Data.Abstractions;
using Fdw.Collections;
using Fdw.Configuration;

namespace Fdw.Commands.Data.Abstractions
;

/// <summary>
/// Non-generic marker interface for configuration command type collections.
/// Implemented by ConfigurationCommandBase&lt;TConfig&gt; so the ConfigurationCommands
/// TypeCollection can enumerate all registered command types regardless of their
/// closed generic parameter.
/// </summary>
public interface IConfigurationCommands : ITypeOption
{
    /// <summary>
    /// Gets the physical table name this command targets.
    /// Used by the configuration providers to look up DataStore container metadata
    /// for FK-based parent ID resolution.
    /// </summary>
    string ContainerName { get; }

    /// <summary>
    /// Gets the concrete configuration type this command targets (the closed <c>TConfig</c>).
    /// </summary>
    /// <remarks>
    /// The save cascade resolves a child's command by identity — <c>ConfigurationCommands.All()
    /// .FirstOrDefault(c =&gt; c.ConfigType == childType)</c> — because the generated
    /// <c>ByName</c>/<c>ById</c> lookups are stubs for this interface-keyed collection (it has no
    /// name/id member). Matching by type is unambiguous and needs no table-name convention.
    /// </remarks>
    Type ConfigType { get; }

    /// <summary>
    /// Creates a configuration save command for <paramref name="record"/>. Non-generic entry point for
    /// the save cascade: the implementation casts <paramref name="record"/> to its concrete config type,
    /// so the cascade saves a runtime-typed child without closing a generic via reflection.
    /// </summary>
    /// <param name="dataStoreName">The data store the record is saved into.</param>
    /// <param name="pathName">The path (schema) within the data store.</param>
    /// <param name="record">The configuration record to save.</param>
    /// <returns>The save command to execute through the gateway.</returns>
    IDataCommand Create(string dataStoreName, string pathName, IGenericConfiguration record);

    /// <summary>
    /// Creates a save command for a record written with field values its properties do not carry --
    /// the Foreign key fields its owner fills with the owner's key values.
    /// </summary>
    /// <param name="dataStoreName">The data store name.</param>
    /// <param name="pathName">The path (schema) name.</param>
    /// <param name="record">The configuration record to save.</param>
    /// <param name="fieldValues">The field values written with the record, keyed by field name.</param>
    /// <returns>The save command.</returns>
    IDataCommand Create(string dataStoreName, string pathName, IGenericConfiguration record, IReadOnlyDictionary<string, object?> fieldValues);

    /// <summary>
    /// Creates an insert of a child record under its owner's row: the Foreign key fields its owner fills
    /// are written from <paramref name="fieldValues"/>, and no current version is retired.
    /// </summary>
    /// <param name="dataStoreName">The data store name.</param>
    /// <param name="pathName">The path (schema) name.</param>
    /// <param name="record">The child record to insert.</param>
    /// <param name="fieldValues">The field values written with the record, keyed by field name.</param>
    /// <returns>The insert command.</returns>
    IDataCommand Insert(string dataStoreName, string pathName, IGenericConfiguration record, IReadOnlyDictionary<string, object?> fieldValues);

    /// <summary>
    /// Creates a configuration soft-delete command for the row identified by <paramref name="id"/>.
    /// Non-generic entry point for the delete cascade, the exact mirror of <see cref="Create(string, string, IGenericConfiguration)"/>.
    /// </summary>
    /// <param name="dataStoreName">The data store the record lives in.</param>
    /// <param name="pathName">The path (schema) within the data store.</param>
    /// <param name="id">The record's own durable logical Id.</param>
    /// <returns>The soft-delete command to execute through the gateway.</returns>
    /// <remarks>
    /// Why: the delete cascade resolves a child's command by the same <c>ConfigType</c> identity the save
    /// cascade uses, so both halves of the walk must be reachable from the non-generic interface. Without
    /// this the cascade could create a child row it had no way to retire.
    /// </remarks>
    IDataCommand Delete(string dataStoreName, string pathName, Guid id);

    /// <summary>
    /// Creates a retire of every current row of this container whose fields hold
    /// <paramref name="fieldValues"/> -- the rows an owner's key values fill the Foreign key of.
    /// </summary>
    /// <param name="dataStoreName">The data store the rows live in.</param>
    /// <param name="pathName">The path the rows live on.</param>
    /// <param name="fieldValues">The field values to match, keyed by field name.</param>
    /// <param name="markDeleted">Whether the retired rows are marked deleted.</param>
    /// <returns>The retire command.</returns>
    IDataCommand Retire(string dataStoreName, string pathName, IReadOnlyDictionary<string, object?> fieldValues, bool markDeleted);
}
