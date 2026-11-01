using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.EtlGateway;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// A step that runs a script a data engineer wrote and keeps how many rows it affected (FDW-1443).
/// </summary>
/// <remarks>
/// The script runs once on a data store through the ETL gateway, with its named parameters made from the variables in scope
/// when the step runs. A failed script fails the step; the run does not go on past it. The gateway holds the caller to the
/// DataEngineer role.
/// </remarks>
public sealed class FlowExecuteSql : IFlowStep
{
    private readonly IEtlGateway _gateway;
    private readonly string _dataStore;
    private readonly string _text;
    private readonly IReadOnlyList<FlowSqlParameter> _parameters;
    private readonly string? _resultVariable;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="FlowExecuteSql"/> class.</summary>
    /// <param name="name">The step's name.</param>
    /// <param name="gateway">The ETL gateway the script runs through.</param>
    /// <param name="dataStore">The data store it runs on.</param>
    /// <param name="text">The command.</param>
    /// <param name="parameters">The values of its named parameters, made from the variables when the step runs.</param>
    /// <param name="resultVariable">A declared variable set to the rows the script affected; null for none.</param>
    /// <param name="logger">The logger.</param>
    public FlowExecuteSql(
        string name,
        IEtlGateway gateway,
        string dataStore,
        string text,
        IReadOnlyList<FlowSqlParameter> parameters,
        string? resultVariable,
        ILogger? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(gateway);
        ArgumentException.ThrowIfNullOrWhiteSpace(dataStore);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ArgumentNullException.ThrowIfNull(parameters);
        Name = name;
        _gateway = gateway;
        _dataStore = dataStore;
        _text = text;
        _parameters = parameters;
        _resultVariable = resultVariable;
        _logger = logger ?? NullLogger.Instance;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public async Task<IGenericResult> Run(FlowStepContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Checked before the script runs: a script that has changed data cannot be taken back by a variable that is not there.
        if (_resultVariable is not null)
        {
            var target = FlowVariableTarget.Check(context.Variables, Name, _resultVariable, _logger);
            if (target.IsFailure)
            {
                return target;
            }
        }

        // Why the text is rendered here: a token names a variable as the steps before this one left it (FDW-1970).
        var text = ScriptTokens.Render(Name, _text, context.Variables.Snapshot(), _logger);
        if (text.IsFailure || text.Value is null)
        {
            return text;
        }

        var values = _parameters.Select(parameter => new ScriptParameter(parameter.Name, parameter.Value(context.Variables))).ToList();
        var ran = await _gateway
            .Execute(new ScriptRunCommand(text.Value, values), _dataStore, cancellationToken)
            .ConfigureAwait(false);
        if (ran.IsFailure)
        {
            return GenericResult.Failure(FlowScriptLog.RunFailed(_logger, Name, _dataStore, FlowFailureReason.Of(ran, _logger, Name, _dataStore)));
        }

        FlowScriptLog.RunFinished(_logger, Name, _dataStore, ran.Value);
        if (_resultVariable is not null)
        {
            context.Variables.TrySet(_resultVariable, ran.Value);
        }

        return GenericResult.Success();
    }
}
