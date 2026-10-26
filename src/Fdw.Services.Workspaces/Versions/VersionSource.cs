using System;
using Fdw.Configuration;
using Fdw.Services.Data.Abstractions;

namespace Fdw.Services.Workspaces;

/// <summary>
/// Where the versions of one kind of definition are read from (FDW-2560): the provider of their rows, the names of the columns that join a row to its
/// definition and its place, and how to read those from a row. A kind of definition that has versions held at places states this once.
/// </summary>
/// <typeparam name="TRow">The domain row of a version.</typeparam>
/// <param name="Provider">Reads the rows.</param>
/// <param name="BaseIdProperty">The property of the row that holds the id of the definition it varies.</param>
/// <param name="ScopeIdProperty">The property of the row that holds the id of the place it is held at.</param>
/// <param name="BaseId">Reads the id of the definition a row varies.</param>
/// <param name="ScopeKind">Reads the kind of place a row is held at.</param>
/// <param name="ScopeId">Reads the id of the place a row is held at.</param>
public sealed record VersionSource<TRow>(
    IDomainRecordProvider<TRow> Provider,
    string BaseIdProperty,
    string ScopeIdProperty,
    Func<TRow, Guid> BaseId,
    Func<TRow, string> ScopeKind,
    Func<TRow, Guid> ScopeId)
    where TRow : class, IDomainConfiguration;
