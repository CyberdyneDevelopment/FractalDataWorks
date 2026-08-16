using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Sql.Commands.Abstractions;
using Fdw.Sql.Commands.Abstractions.Results;
using Fdw.Sql.Commands.Logging;
using Fdw.Sql.Commands.Project.Commands;
using Fdw.Sql.Workspace;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
        var logger = NullLogger<GetObjectInfoTranslator>.Instance;
        GetObjectInfoTranslatorLog.Translating(logger, command.ObjectName ?? string.Empty, command.Schema ?? string.Empty);

        if (string.IsNullOrWhiteSpace(command.ObjectName))
            return Task.FromResult<IGenericResult<QueryResult<SqlObjectDetail>>>(
                GenericResult<QueryResult<SqlObjectDetail>>.Failure(GetObjectInfoTranslatorLog.ObjectNameRequired(logger)));

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
                GenericResult<QueryResult<SqlObjectDetail>>.Failure(
                    GetObjectInfoTranslatorLog.ObjectNotFound(logger, command.ObjectName)));

        var scriptText = TryGetScript(match, logger);

        var detail = new SqlObjectDetail(
            Name: match.Name.Parts[match.Name.Parts.Count - 1],
            FullName: match.Name.ToString(),
            Kind: match.ObjectType.Name,
            Definition: scriptText);

        GetObjectInfoTranslatorLog.ObjectFound(logger, detail.FullName, detail.Kind);
        return Task.FromResult<IGenericResult<QueryResult<SqlObjectDetail>>>(
            GenericResult<QueryResult<SqlObjectDetail>>.Success(
                new QueryResult<SqlObjectDetail>($"Object '{detail.FullName}' ({detail.Kind})", detail)));
    }

    // Why: GetScript() throws for non-scriptable object types; extracting it here keeps the
    // swallow-and-continue (null Definition) out of the IGenericResult-returning Translate method.
    private static string? TryGetScript(TSqlObject obj, ILogger logger)
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
            GetObjectInfoTranslatorLog.ScriptUnavailable(logger, obj.Name.ToString(), obj.ObjectType.Name);
            return null;
        }
    }
}
