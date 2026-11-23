using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Options;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Pipelines.Loads;
using Fdw.Services.EtlGateway;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// A step that reads the data configuration rules of a column of a data container and keeps the list of values they pick in a variable (FDW-5347, FDW-6344).
/// </summary>
/// <remarks>
/// <para>
/// The current, not deleted, active rules of the column are read once, with their current, not deleted values, in one query through the ETL gateway
/// (never a call for each rule), and applied in memory. A rule is a selection of values from the column; what a consumer does with it is the consumer's
/// decision, and this step's is a list: a rule of kind <c>In</c> puts each of its values in the list and a rule of kind <c>NotIn</c> takes each out,
/// through the select-list options (<c>Include</c> and <c>Exclude</c>), so a value is never in the list twice and every <c>Include</c> is applied before
/// every <c>Exclude</c>, whatever order the store returned the rows in. The list starts empty.
/// </para>
/// <para>
/// A rule's stored path (<c>DataConfigurationRules:Comparison:In.To</c>) is resolved to its kind with <see cref="RulePaths"/>; a path that does not resolve
/// fails the step. A rule of any other kind is not a list of picked values and fails the step naming the rule and its path. A value of kind Parameter is the
/// value the run holds for the parameter its <c>Parameter</c> field names: the step builds the reference <c>{{Name}}</c> and resolves it with
/// <see cref="PipelineRunParameterReferences.Resolve"/> against the run's values (FDW-6611), so it is placed by the rule every expression of a run follows; a
/// parameter the run does not carry, left blank, or holding a value that cannot be placed, fails the step with the resolver's coded message, never an empty or
/// default value. A literal value is placed as stored, even when it holds braces. Nothing is skipped. The gateway holds the caller to the DataEngineer role and
/// refuses a store the host declares; a failed read fails the step, and the variable is not set.
/// </para>
/// </remarks>
public sealed class FlowReadDataOptions : IFlowStep
{
    // The values are bound parameters, and so are the flags (a bit is not written as 1 or 0 in text, which a store that has a real boolean refuses). The names that
    // change, the two containers, are the tokens $(RulesContainer) and $(ValuesContainer), which ScriptTokens holds to letters, digits, underscore and dot. The fields
    // are written in double quotes because Column is a reserved word in some dialects. A value belongs to a rule by the rule's durable Id, as the tables state.
    private const string RulesToken = "RulesContainer";
    private const string ValuesToken = "ValuesContainer";

    private const string QueryText =
        "SELECT r.\"Id\" AS \"RuleId\", r.\"Name\" AS \"RuleName\", r.\"Path\" AS \"RulePath\", v.\"Kind\" AS \"ValueKind\", v.\"Value\" AS \"Value\", "
        + "v.\"Parameter\" AS \"Parameter\", v.\"Ordinal\" AS \"Ordinal\" "
        + "FROM $(" + RulesToken + ") r INNER JOIN $(" + ValuesToken + ") v ON v.\"DataConfigurationRuleId\" = r.\"Id\" "
        + "AND v.\"IsCurrent\" = @IsCurrent AND v.\"IsDeleted\" = @IsDeleted "
        + "WHERE r.\"DataStore\" = @DataStore AND r.\"DataPath\" = @DataPath AND r.\"Container\" = @Container AND r.\"Column\" = @Column "
        + "AND r.\"Active\" = @Active AND r.\"IsCurrent\" = @IsCurrent AND r.\"IsDeleted\" = @IsDeleted "
        + "ORDER BY r.\"Id\", v.\"Ordinal\"";

    private readonly IEtlGateway _gateway;
    private readonly string _optionsDataStore;
    private readonly string _rulesContainer;
    private readonly string _valuesContainer;
    private readonly string _dataStore;
    private readonly string _path;
    private readonly string _container;
    private readonly string _column;
    private readonly int _maxRows;
    private readonly string _resultVariable;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="FlowReadDataOptions"/> class.</summary>
    /// <param name="name">The step's name.</param>
    /// <param name="gateway">The ETL gateway the read runs through.</param>
    /// <param name="optionsDataStore">The data store that holds the rule tables.</param>
    /// <param name="rulesContainer">The table that holds the rules, as it is named in the data store.</param>
    /// <param name="valuesContainer">The table that holds the rules' values, as it is named in the data store.</param>
    /// <param name="dataStore">The data store the rules are about.</param>
    /// <param name="path">The data path of the container the rules are about.</param>
    /// <param name="container">The data container the rules are about.</param>
    /// <param name="column">The column the rules are about.</param>
    /// <param name="maxRows">The most value rows the column's rules may have; at least one.</param>
    /// <param name="resultVariable">The declared variable the list of values is kept in.</param>
    /// <param name="logger">The logger.</param>
    public FlowReadDataOptions(
        string name,
        IEtlGateway gateway,
        string optionsDataStore,
        string rulesContainer,
        string valuesContainer,
        string dataStore,
        string path,
        string container,
        string column,
        int maxRows,
        string resultVariable,
        ILogger? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(optionsDataStore);
        ArgumentException.ThrowIfNullOrWhiteSpace(rulesContainer);
        ArgumentException.ThrowIfNullOrWhiteSpace(valuesContainer);
        ArgumentException.ThrowIfNullOrWhiteSpace(dataStore);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(container);
        ArgumentException.ThrowIfNullOrWhiteSpace(column);
        ArgumentException.ThrowIfNullOrWhiteSpace(resultVariable);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxRows, 1);
        Name = name;
        _gateway = gateway;
        _optionsDataStore = optionsDataStore;
        _rulesContainer = rulesContainer;
        _valuesContainer = valuesContainer;
        _dataStore = dataStore;
        _path = path;
        _container = container;
        _column = column;
        _maxRows = maxRows;
        _resultVariable = resultVariable;
        _logger = logger ?? NullLogger.Instance;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public async Task<IGenericResult> Run(FlowStepContext context, CancellationToken cancellationToken = default)
    {
        var target = FlowVariableTarget.Check(context.Variables, Name, _resultVariable, _logger);
        if (target.IsFailure)
        {
            return target;
        }

        var rows = await Read(cancellationToken).ConfigureAwait(false);
        if (rows.IsFailure || rows.Value is null)
        {
            return rows;
        }

        var applied = Apply(rows.Value, context.Variables.Snapshot());
        if (applied.IsFailure || applied.Value is null)
        {
            return applied;
        }

        context.Variables.TrySet(_resultVariable, applied.Value);
        FlowDataOptionsLog.Read(_logger, Name, rows.Value.Count, _column, _container, applied.Value.Count, _resultVariable);
        return GenericResult.Success();
    }

    // One query for every value of every current, active rule of the column; the containers' names are rendered by ScriptTokens, which refuses anything but a name.
    private async Task<IGenericResult<IReadOnlyList<RuleValueRow>>> Read(CancellationToken cancellationToken)
    {
        var text = ScriptTokens.Render(
            Name,
            QueryText,
            new Dictionary<string, object?>(StringComparer.Ordinal) { [RulesToken] = _rulesContainer, [ValuesToken] = _valuesContainer },
            _logger);
        if (text.IsFailure || text.Value is null)
        {
            return text.ToNewResult<IReadOnlyList<RuleValueRow>>();
        }

        ScriptParameter[] parameters =
        [
            new("DataStore", _dataStore),
            new("DataPath", _path),
            new("Container", _container),
            new("Column", _column),
            new("Active", true),
            new("IsCurrent", true),
            new("IsDeleted", false),
        ];

        // Why one more than the most: the row after the most is what shows the column has too many; this step fails on it with its own message.
        var opened = await _gateway
            .Execute(new ScriptStreamCommand(text.Value, parameters, _maxRows + 1), _optionsDataStore, cancellationToken)
            .ConfigureAwait(false);
        if (opened.IsFailure)
        {
            return FlowFailures.Caused<IReadOnlyList<RuleValueRow>>(
                FlowDataOptionsLog.ReadFailed(_logger, Name, _optionsDataStore, _rulesContainer, FlowFailureReason.Of(opened, _logger, Name, _optionsDataStore)), opened);
        }

        if (opened.Value is not { } source)
        {
            return GenericResult<IReadOnlyList<RuleValueRow>>.Failure(FlowDataOptionsLog.ReadReturnedNothing(_logger, Name, _optionsDataStore, _rulesContainer));
        }

        await using (source.ConfigureAwait(false))
        {
            return await Collect(source, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<IGenericResult<IReadOnlyList<RuleValueRow>>> Collect(IRecordSource<DataRecord> source, CancellationToken cancellationToken)
    {
        List<RuleValueRow> rows = [];
        await foreach (var record in source.Read(cancellationToken).ConfigureAwait(false))
        {
            if (record.IsFailure)
            {
                return FlowFailures.Caused<IReadOnlyList<RuleValueRow>>(
                    FlowDataOptionsLog.ReadFailed(_logger, Name, _optionsDataStore, _rulesContainer, FlowFailureReason.Of(record, _logger, Name, _optionsDataStore)), record);
            }

            if (rows.Count == _maxRows)
            {
                return GenericResult<IReadOnlyList<RuleValueRow>>.Failure(
                    FlowDataOptionsLog.TooManyRows(_logger, Name, _optionsDataStore, _rulesContainer, _maxRows));
            }

            var row = RuleValueRow.Of(record.Value);
            if (row.Reason is { } reason)
            {
                return GenericResult<IReadOnlyList<RuleValueRow>>.Failure(FlowDataOptionsLog.RowInvalid(_logger, Name, _rulesContainer, rows.Count + 1, reason));
            }

            rows.Add(row);
        }

        return GenericResult<IReadOnlyList<RuleValueRow>>.Success(rows);
    }

    // Applies every Include before every Exclude, each on the values the ones before it left, and within each the rows in the rule's and value's own order, so the list is
    // the same whatever order the store returned them in.
    private IGenericResult<IReadOnlyList<string>> Apply(IReadOnlyList<RuleValueRow> rows, IReadOnlyDictionary<string, object?> run)
    {
        List<(RuleValueRow Row, IDataOption Option, string? Value)> chosen = [];
        foreach (var row in rows)
        {
            var option = OptionOf(row);
            if (option.IsFailure || option.Value is null)
            {
                return option.ToNewResult<IReadOnlyList<string>>();
            }

            var value = ValueOf(row, run);
            if (value.IsFailure)
            {
                return value.ToNewResult<IReadOnlyList<string>>();
            }

            chosen.Add((row, option.Value, value.Value));
        }

        IReadOnlyList<string> values = [];
        foreach (var (row, option, value) in chosen
            .OrderBy(item => item.Option.ApplyOrder)
            .ThenBy(item => item.Row.RuleId, StringComparer.Ordinal)
            .ThenBy(item => item.Row.Ordinal))
        {
            var applied = option.Apply(new DataOptionContext(_dataStore, _container, _column, DataOptions.SelectList().Name, option.Name, value, true, values, _logger));
            if (applied.IsFailure)
            {
                return FlowFailures.Caused<IReadOnlyList<string>>(
                    FlowDataOptionsLog.ApplyFailed(_logger, Name, DataOptions.SelectList().Name, option.Name, _column, _rulesContainer, FlowFailures.ReasonOf(applied)), applied);
            }

            if (applied.Value is not SelectListOutcome outcome)
            {
                return GenericResult<IReadOnlyList<string>>.Failure(FlowDataOptionsLog.OutcomeNotAList(_logger, Name, DataOptions.SelectList().Name, option.Name));
            }

            values = outcome.Values;
        }

        return GenericResult<IReadOnlyList<string>>.Success(values);
    }

    // The select-list option a rule value stands for: its path resolves to a kind, In is Include and NotIn is Exclude.
    private IGenericResult<IDataOption> OptionOf(RuleValueRow row)
    {
        if (!RulePaths.TrySplit(row.RulePath, out var path, out var method))
        {
            return GenericResult<IDataOption>.Failure(FlowDataOptionsLog.RuleRowInvalid(_logger, Name, _rulesContainer, row.RuleName, "RulePath is not a path and a method"));
        }

        var kind = RulePaths.Resolve(_logger, path, method);
        if (kind.IsFailure || kind.Value is null)
        {
            return FlowFailures.Caused<IDataOption>(FlowDataOptionsLog.RulePathUnresolved(_logger, Name, row.RuleName, row.RulePath, FlowFailures.ReasonOf(kind)), kind);
        }

        if (ReferenceEquals(DataOptionValueKinds.ByName(row.ValueKind), DataOptionValueKinds.NotFound))
        {
            return GenericResult<IDataOption>.Failure(FlowDataOptionsLog.RuleRowInvalid(_logger, Name, _rulesContainer, row.RuleName, "ValueKind is not a value kind"));
        }

        // The select-list option states the comparison it stands for; a comparison no option stands for is not a list of picked values.
        return ReferenceEquals(SelectListOptions.ByComparison(kind.Value.Name), SelectListOptions.NotFound)
            ? GenericResult<IDataOption>.Failure(FlowDataOptionsLog.RuleKindNotAList(_logger, Name, row.RuleName, row.RulePath))
            : GenericResult<IDataOption>.Success(SelectListOptions.ByComparison(kind.Value.Name));
    }

    // The text a rule's value stands for: a literal as stored; a parameter the value the run holds for it, placed by the resolver every expression of a run uses (FDW-6611).
    // The reference is built from the stored name, and a name the reference grammar would not read as one is refused here, because the resolver would leave that text as it is.
    private IGenericResult<string?> ValueOf(RuleValueRow row, IReadOnlyDictionary<string, object?> run)
    {
        if (!ReferenceEquals(DataOptionValueKinds.ByName(row.ValueKind), DataOptionValueKinds.Parameter))
        {
            return GenericResult<string?>.Success(row.Value);
        }

        if (string.IsNullOrWhiteSpace(row.Parameter))
        {
            return GenericResult<string?>.Failure(FlowDataOptionsLog.RuleRowInvalid(_logger, Name, _rulesContainer, row.RuleName, "Parameter is missing"));
        }

        var reference = "{{" + row.Parameter + "}}";
        if (!PipelineRunParameterReferences.Names(reference).SequenceEqual([row.Parameter], StringComparer.OrdinalIgnoreCase))
        {
            return GenericResult<string?>.Failure(FlowDataOptionsLog.RuleRowInvalid(_logger, Name, _rulesContainer, row.RuleName, "Parameter is not a parameter name"));
        }

        var resolved = PipelineRunParameterReferences.Resolve($"flow step '{Name}', rule '{row.RuleName}'", reference, run, _logger);
        if (resolved.IsFailure)
        {
            return resolved.ToNewResult<string?>();
        }

        FlowDataOptionsLog.ParameterResolved(_logger, Name, row.RuleName, row.Parameter);
        return GenericResult<string?>.Success(resolved.Value);
    }

    // One stored value of a rule as the step reads it: the rule's Id, name and path are required text, the value kind is required text, the value and parameter are text or none,
    // and the ordinal is a whole number.
    private sealed record RuleValueRow(string RuleId, string RuleName, string RulePath, string ValueKind, string? Value, string? Parameter, int Ordinal, string? Reason)
    {
        internal static RuleValueRow Of(DataRecord record)
        {
            var fields = new Dictionary<string, object?>(record.FieldCount, StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < record.FieldCount; i++)
            {
                fields[record.Schema.GetFieldName(i)] = record[i];
            }

            return Invalid(fields, "RuleId") is { } id ? Failed(id)
                : Invalid(fields, "RuleName") is { } name ? Failed(name)
                : Invalid(fields, "RulePath") is { } path ? Failed(path)
                : Invalid(fields, "ValueKind") is { } kind ? Failed(kind)
                : !TryWhole(fields, out var ordinal) ? Failed("Ordinal is not a whole number")
                : new RuleValueRow(
                    Text(fields, "RuleId")!,
                    Text(fields, "RuleName")!,
                    Text(fields, "RulePath")!,
                    Text(fields, "ValueKind")!,
                    Text(fields, "Value"),
                    Text(fields, "Parameter"),
                    ordinal,
                    null);
        }

        private static RuleValueRow Failed(string reason) => new(string.Empty, string.Empty, string.Empty, string.Empty, null, null, 0, reason);

        private static string? Text(Dictionary<string, object?> fields, string field) =>
            fields.TryGetValue(field, out var value) && value is not null ? Convert.ToString(value, CultureInfo.InvariantCulture) : null;

        // Why the fields are checked and not assumed: a missing name could only be filled by guessing which rule the row meant.
        private static string? Invalid(Dictionary<string, object?> fields, string field) =>
            string.IsNullOrWhiteSpace(Text(fields, field)) ? $"{field} is missing" : null;

        private static bool TryWhole(Dictionary<string, object?> fields, out int ordinal)
        {
            ordinal = 0;
            return fields.TryGetValue("Ordinal", out var value)
                && value is not null
                && Convert.GetTypeCode(value) is >= TypeCode.SByte and <= TypeCode.UInt32
                && int.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out ordinal);
        }
    }
}
