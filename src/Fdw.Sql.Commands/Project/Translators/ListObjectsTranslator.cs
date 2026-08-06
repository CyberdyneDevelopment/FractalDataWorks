using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Sql.Commands.Abstractions;
using Fdw.Sql.Commands.Abstractions.Results;
using Fdw.Sql.Commands.Project.Commands;
using Fdw.Sql.Workspace;
using Microsoft.SqlServer.Dac.Model;

namespace Fdw.Sql.Commands.Project.Translators;

/// <summary>Lists every object in the workspace's TSqlModel, optionally filtered by kind and/or schema.</summary>
[TypeOption(typeof(SqlProjectTranslators), "ListObjects", RestrictToCurrentCompilation = true)]
public sealed class ListObjectsTranslator : SqlCommandTranslatorBase<ListObjectsCommand, QueryResult<IReadOnlyList<SqlObjectInfo>>>
{
    public ListObjectsTranslator()
        : base("ListObjects", "Enumerates the workspace TSqlModel.")
    {
    }

    /// <inheritdoc/>
    public override Task<IGenericResult<QueryResult<IReadOnlyList<SqlObjectInfo>>>> Translate(
        ListObjectsCommand command,
        ISqlWorkspace workspace,
        CancellationToken cancellationToken = default)
    {
        var kindFilter = ResolveKind(command.ObjectKind);
        var schemaFilter = command.Schema;

        IEnumerable<TSqlObject> objects = kindFilter is null
            ? workspace.Model.GetObjects(DacQueryScopes.Default)
            : workspace.Model.GetObjects(DacQueryScopes.Default, kindFilter);

        var results = new List<SqlObjectInfo>();
        foreach (var obj in objects)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var parts = obj.Name.Parts;
            var schema = parts.Count >= 2 ? parts[parts.Count - 2] : null;
            var name = parts.Count >= 1 ? parts[parts.Count - 1] : obj.Name.ToString();

            if (!string.IsNullOrWhiteSpace(schemaFilter) && !string.Equals(schema, schemaFilter, StringComparison.OrdinalIgnoreCase))
                continue;

            results.Add(new SqlObjectInfo(
                Name: name,
                Schema: schema ?? string.Empty,
                Kind: obj.ObjectType.Name,
                FullName: obj.Name.ToString()));
        }

        var result = new QueryResult<IReadOnlyList<SqlObjectInfo>>(
            $"Found {results.Count} object(s)",
            results.OrderBy(o => o.Schema, StringComparer.OrdinalIgnoreCase)
                   .ThenBy(o => o.Kind, StringComparer.OrdinalIgnoreCase)
                   .ThenBy(o => o.Name, StringComparer.OrdinalIgnoreCase)
                   .ToList());

        return Task.FromResult<IGenericResult<QueryResult<IReadOnlyList<SqlObjectInfo>>>>(
            GenericResult<QueryResult<IReadOnlyList<SqlObjectInfo>>>.Success(result));
    }

    private static ModelTypeClass? ResolveKind(string? kind)
    {
        if (string.IsNullOrWhiteSpace(kind)) return null;
        return kind.ToUpperInvariant() switch
        {
            "TABLE" => ModelSchema.Table,
            "VIEW" => ModelSchema.View,
            "PROCEDURE" or "PROC" => ModelSchema.Procedure,
            "FUNCTION" or "FUNC" => ModelSchema.ScalarFunction,
            "TYPE" => ModelSchema.UserDefinedType,
            "SCHEMA" => ModelSchema.Schema,
            _ => null
        };
    }
}
