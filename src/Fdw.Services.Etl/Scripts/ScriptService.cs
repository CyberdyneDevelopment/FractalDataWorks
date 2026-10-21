using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.EtlGateway;
using Fdw.Services.Etl.Graphs.Building;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Scripts;

/// <summary>
/// Runs a script once through the ETL gateway (FDW-1442), with the values of its declared parameters. The gateway holds the caller
/// to the DataEngineer role; a query states its own most rows. This reads the values and names what is missing or unknown.
/// </summary>
public sealed class ScriptService : IScriptService
{
    private readonly IEtlGateway _gateway;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="ScriptService"/> class.</summary>
    /// <param name="gateway">The ETL gateway the script runs through.</param>
    /// <param name="logger">The logger.</param>
    public ScriptService(IEtlGateway gateway, ILogger<ScriptService>? logger = null)
    {
        _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
        _logger = logger ?? NullLogger<ScriptService>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<ScriptResultPayload>> Run(ScriptPayload script, ScriptRunPayload run, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(script);
        ArgumentNullException.ThrowIfNull(run);
        var kind = ScriptKinds.ByName(script.Kind);
        if (ReferenceEquals(kind, ScriptKinds.NotFound))
        {
            return ScriptRefusal.Of<ScriptResultPayload>($"the script '{script.Name}' is of kind '{script.Kind}', which is neither Query nor NonQuery");
        }

        var parameters = Values(script, run);
        if (parameters.IsFailure || parameters.Value is null)
        {
            return parameters.ToNewResult<ScriptResultPayload>();
        }

        ScriptLog.RunStarted(_logger, script.Name, kind.Name, script.Connection, parameters.Value.Count);
        return kind.AnswersRows
            ? await Query(script, run, parameters.Value, cancellationToken).ConfigureAwait(false)
            : await NonQuery(script, run, parameters.Value, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IGenericResult<ScriptResultPayload>> Query(
        ScriptPayload script, ScriptRunPayload run, IReadOnlyList<ScriptParameter> parameters, CancellationToken ct)
    {
        if (run.MaxRows is not { } maxRows)
        {
            return ScriptRefusal.Of<ScriptResultPayload>($"the query '{script.Name}' is run without saying the most rows to read (maxRows)");
        }

        // One row more than asked for is read, so an answer that stops at the most rows can say whether the query had more.
        var read = await _gateway
            .Execute(new ScriptReadCommand(script.Text, parameters, maxRows + 1), script.Connection, ct)
            .ConfigureAwait(false);
        if (read.IsFailure || read.Value is null)
        {
            ScriptLog.RunFailed(_logger, script.Name, script.Connection, read.CurrentMessage);
            return read.ToNewResult<ScriptResultPayload>();
        }

        var rows = read.Value.Take(maxRows).ToList();
        ScriptLog.RunFinished(_logger, script.Name, script.Kind, script.Connection, rows.Count);
        return GenericResult<ScriptResultPayload>.Success(new ScriptResultPayload
        {
            Kind = script.Kind,
            Columns = Columns(rows),
            Rows = rows,
            HasMore = read.Value.Count > maxRows,
        });
    }

    private static List<ScriptColumnPayload> Columns(List<IDictionary<string, object?>> rows) =>
        rows.Count == 0
            ? []
            : [.. rows[0].Keys.Select(name => new ScriptColumnPayload
            {
                Name = name,
                Type = rows.Select(row => row[name]).FirstOrDefault(value => value is not null)?.GetType().Name,
            })];

    private async Task<IGenericResult<ScriptResultPayload>> NonQuery(
        ScriptPayload script, ScriptRunPayload run, IReadOnlyList<ScriptParameter> parameters, CancellationToken ct)
    {
        if (run.MaxRows is not null)
        {
            return ScriptRefusal.Of<ScriptResultPayload>($"the non-query '{script.Name}' reads no rows, so it has no maxRows");
        }

        var ran = await _gateway
            .Execute(new ScriptRunCommand(script.Text, parameters), script.Connection, ct)
            .ConfigureAwait(false);
        if (ran.IsFailure)
        {
            ScriptLog.RunFailed(_logger, script.Name, script.Connection, ran.CurrentMessage);
            return ran.ToNewResult<ScriptResultPayload>();
        }

        ScriptLog.RunFinished(_logger, script.Name, script.Kind, script.Connection, ran.Value);
        return GenericResult<ScriptResultPayload>.Success(new ScriptResultPayload { Kind = script.Kind, RowsAffected = ran.Value });
    }

    private static IGenericResult<IReadOnlyList<ScriptParameter>> Values(ScriptPayload script, ScriptRunPayload run)
    {
        var twice = run.Values.Keys.GroupBy(name => name, StringComparer.OrdinalIgnoreCase).FirstOrDefault(group => group.Count() > 1);
        if (twice is not null)
        {
            return ScriptRefusal.Of<IReadOnlyList<ScriptParameter>>($"the value of the parameter '{twice.Key}' is given twice");
        }

        var given = new Dictionary<string, string?>(run.Values, StringComparer.OrdinalIgnoreCase);
        var declared = script.Parameters.Select(parameter => parameter.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var unknown = given.Keys.FirstOrDefault(name => !declared.Contains(name));
        if (unknown is not null)
        {
            return ScriptRefusal.Of<IReadOnlyList<ScriptParameter>>($"the script '{script.Name}' declares no parameter '{unknown}'");
        }

        List<ScriptParameter> parameters = [];
        foreach (var parameter in script.Parameters)
        {
            var one = Value(script, parameter, given.TryGetValue(parameter.Name, out var text) ? text : null);
            if (one.IsFailure || one.Value is null)
            {
                return one.ToNewResult<IReadOnlyList<ScriptParameter>>();
            }

            parameters.Add(one.Value);
        }

        return GenericResult<IReadOnlyList<ScriptParameter>>.Success(parameters);
    }

    private static IGenericResult<ScriptParameter> Value(ScriptPayload script, ScriptParameterPayload parameter, string? text)
    {
        if (text is null && parameter.Required)
        {
            return ScriptRefusal.Of<ScriptParameter>($"the parameter '{parameter.Name}' of '{script.Name}' is required and no value was given");
        }

        return GraphValues.TryRead(parameter.Type, text, out var value)
            ? GenericResult<ScriptParameter>.Success(new ScriptParameter(parameter.Name, value))
            : ScriptRefusal.Of<ScriptParameter>($"the value '{text}' of the parameter '{parameter.Name}' cannot be read as {parameter.Type}");
    }
}
