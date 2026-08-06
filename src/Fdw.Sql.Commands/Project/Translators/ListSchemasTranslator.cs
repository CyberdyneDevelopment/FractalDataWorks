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

[TypeOption(typeof(SqlProjectTranslators), "ListSchemas", RestrictToCurrentCompilation = true)]
public sealed class ListSchemasTranslator : SqlCommandTranslatorBase<ListSchemasCommand, QueryResult<IReadOnlyList<string>>>
{
    public ListSchemasTranslator() : base("ListSchemas", "Lists schemas in the workspace model.") { }

    public override Task<IGenericResult<QueryResult<IReadOnlyList<string>>>> Translate(
        ListSchemasCommand command,
        ISqlWorkspace workspace,
        CancellationToken cancellationToken = default)
    {
        var schemas = workspace.Model.GetObjects(DacQueryScopes.Default, ModelSchema.Schema)
            .Select(o => o.Name.Parts[o.Name.Parts.Count - 1])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Task.FromResult<IGenericResult<QueryResult<IReadOnlyList<string>>>>(
            GenericResult<QueryResult<IReadOnlyList<string>>>.Success(
                new QueryResult<IReadOnlyList<string>>($"Found {schemas.Count} schema(s)", schemas)));
    }
}
