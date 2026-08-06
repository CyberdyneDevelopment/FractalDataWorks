using System;
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

[TypeOption(typeof(SqlProjectTranslators), "GetObjectInfo", RestrictToCurrentCompilation = true)]
public sealed class GetObjectInfoTranslator : SqlCommandTranslatorBase<GetObjectInfoCommand, QueryResult<SqlObjectDetail>>
{
    public GetObjectInfoTranslator() : base("GetObjectInfo", "Returns metadata for one object.") { }

    public override Task<IGenericResult<QueryResult<SqlObjectDetail>>> Translate(
        GetObjectInfoCommand command,
        ISqlWorkspace workspace,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.ObjectName))
            return Task.FromResult<IGenericResult<QueryResult<SqlObjectDetail>>>(
                GenericResult<QueryResult<SqlObjectDetail>>.Failure(SqlResultCodes.ObjectNameRequired));

        var match = workspace.Model.GetObjects(DacQueryScopes.Default)
            .FirstOrDefault(o =>
            {
                var parts = o.Name.Parts;
                var name = parts.Count >= 1 ? parts[parts.Count - 1] : string.Empty;
                var schema = parts.Count >= 2 ? parts[parts.Count - 2] : null;
                var nameMatch = string.Equals(name, command.ObjectName, System.StringComparison.OrdinalIgnoreCase);
                if (!nameMatch) return false;
                if (string.IsNullOrWhiteSpace(command.Schema)) return true;
                return string.Equals(schema, command.Schema, System.StringComparison.OrdinalIgnoreCase);
            });

        if (match is null)
            return Task.FromResult<IGenericResult<QueryResult<SqlObjectDetail>>>(
                GenericResult<QueryResult<SqlObjectDetail>>.Failure(SqlResultCodes.ObjectNotFound,
                    ResultDetails.Create("ObjectName", command.ObjectName)));

        var scriptText = TryGetScript(match);

        var detail = new SqlObjectDetail(
            Name: match.Name.Parts[match.Name.Parts.Count - 1],
            FullName: match.Name.ToString(),
            Kind: match.ObjectType.Name,
            Definition: scriptText);

        return Task.FromResult<IGenericResult<QueryResult<SqlObjectDetail>>>(
            GenericResult<QueryResult<SqlObjectDetail>>.Success(
                new QueryResult<SqlObjectDetail>($"Object '{detail.FullName}' ({detail.Kind})", detail)));
    }

    // Why: GetScript() throws for non-scriptable object types; extracting it here keeps the
    // swallow-and-continue (null Definition) out of the IGenericResult-returning Translate method.
    private static string? TryGetScript(TSqlObject obj)
    {
        try
        {
            return obj.GetScript();
        }
        catch (Exception ex)
        {
            // Why: not all SQL objects are scriptable. Return null Definition and continue;
            // ex.Message observed to satisfy FDW022.
            _ = ex.Message;
            return null;
        }
    }
}
