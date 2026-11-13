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
using Fdw.Services.EtlGateway;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// A step that reads the data options stored for a column of a data container, applies them, and keeps the list of values they leave in
/// a variable (FDW-5347).
/// </summary>
/// <remarks>
/// <para>
/// The rows are read once, in one query through the ETL gateway, and applied in memory: never a call for each row. A row names an option
/// type and an option as text, resolved as <c>DataOptions.ByName(type).ByName(option)</c>; a name that is not known fails the step, and
/// nothing is skipped. The rows are applied in the options' own order (<see cref="IDataOption.ApplyOrder"/>), so the list does not depend
/// on the order the store returned them in; each option builds on the values the ones before it left.
/// </para>
/// <para>
/// The fields of a row are <see cref="DataOptionRowFields"/>. The gateway holds the caller to the DataEngineer role and refuses a store
/// the host declares; a failed read fails the step, and the variable is not set.
/// </para>
/// </remarks>
public sealed class FlowReadDataOptions : IFlowStep
{
    // The values are bound parameters; the one name that changes, the container, is the token $(OptionsContainer), which ScriptTokens holds to letters,
    // digits, underscore and dot. The fields are written in double quotes because Column and Option are reserved words in some dialects.
    private const string ContainerToken = "OptionsContainer";

    private const string QueryText =
        "SELECT \"Value\", \"OptionType\", \"Option\", \"Active\" FROM $(" + ContainerToken + ") "
        + "WHERE \"DataStore\" = @DataStore AND \"Container\" = @Container AND \"Column\" = @Column";

    private readonly IEtlGateway _gateway;
    private readonly string _optionsDataStore;
    private readonly string _optionsContainer;
    private readonly string _dataStore;
    private readonly string _container;
    private readonly string _column;
    private readonly int _maxRows;
    private readonly string _resultVariable;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="FlowReadDataOptions"/> class.</summary>
    /// <param name="name">The step's name.</param>
    /// <param name="gateway">The ETL gateway the read runs through.</param>
    /// <param name="optionsDataStore">The data store that holds the stored options.</param>
    /// <param name="optionsContainer">The container that holds the stored options.</param>
    /// <param name="dataStore">The data store the options are about.</param>
    /// <param name="container">The data container the options are about.</param>
    /// <param name="column">The column the options are about.</param>
    /// <param name="maxRows">The most option rows the column may have; at least one.</param>
    /// <param name="resultVariable">The declared variable the list of values is kept in.</param>
    /// <param name="logger">The logger.</param>
    public FlowReadDataOptions(
        string name,
        IEtlGateway gateway,
        string optionsDataStore,
        string optionsContainer,
        string dataStore,
        string container,
        string column,
        int maxRows,
        string resultVariable,
        ILogger? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(optionsDataStore);
        ArgumentException.ThrowIfNullOrWhiteSpace(optionsContainer);
        ArgumentException.ThrowIfNullOrWhiteSpace(dataStore);
        ArgumentException.ThrowIfNullOrWhiteSpace(container);
        ArgumentException.ThrowIfNullOrWhiteSpace(column);
        ArgumentException.ThrowIfNullOrWhiteSpace(resultVariable);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxRows, 1);
        Name = name;
        _gateway = gateway;
        _optionsDataStore = optionsDataStore;
        _optionsContainer = optionsContainer;
        _dataStore = dataStore;
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

        var applied = Apply(rows.Value);
        if (applied.IsFailure || applied.Value is null)
        {
            return applied;
        }

        context.Variables.TrySet(_resultVariable, applied.Value);
        FlowDataOptionsLog.Read(_logger, Name, rows.Value.Count, _column, _container, applied.Value.Count, _resultVariable);
        return GenericResult.Success();
    }

    // One query for every option row of the column; the container's name is rendered by ScriptTokens, which refuses anything but a name.
    private async Task<IGenericResult<IReadOnlyList<OptionRow>>> Read(CancellationToken cancellationToken)
    {
        var text = ScriptTokens.Render(Name, QueryText, new Dictionary<string, object?>(StringComparer.Ordinal) { [ContainerToken] = _optionsContainer }, _logger);
        if (text.IsFailure || text.Value is null)
        {
            return text.ToNewResult<IReadOnlyList<OptionRow>>();
        }

        ScriptParameter[] parameters =
        [
            new("DataStore", _dataStore),
            new("Container", _container),
            new("Column", _column),
        ];

        // Why one more than the most: the row after the most is what shows the column has too many; this step fails on it with its own message.
        var opened = await _gateway
            .Execute(new ScriptStreamCommand(text.Value, parameters, _maxRows + 1), _optionsDataStore, cancellationToken)
            .ConfigureAwait(false);
        if (opened.IsFailure)
        {
            return FlowFailures.Caused<IReadOnlyList<OptionRow>>(
                FlowDataOptionsLog.ReadFailed(_logger, Name, _optionsDataStore, _optionsContainer, FlowFailureReason.Of(opened, _logger, Name, _optionsDataStore)), opened);
        }

        if (opened.Value is not { } source)
        {
            return GenericResult<IReadOnlyList<OptionRow>>.Failure(FlowDataOptionsLog.ReadReturnedNothing(_logger, Name, _optionsDataStore, _optionsContainer));
        }

        await using (source.ConfigureAwait(false))
        {
            return await Collect(source, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<IGenericResult<IReadOnlyList<OptionRow>>> Collect(IRecordSource<DataRecord> source, CancellationToken cancellationToken)
    {
        List<OptionRow> rows = [];
        await foreach (var record in source.Read(cancellationToken).ConfigureAwait(false))
        {
            if (record.IsFailure)
            {
                return FlowFailures.Caused<IReadOnlyList<OptionRow>>(
                    FlowDataOptionsLog.ReadFailed(_logger, Name, _optionsDataStore, _optionsContainer, FlowFailureReason.Of(record, _logger, Name, _optionsDataStore)), record);
            }

            if (rows.Count == _maxRows)
            {
                return GenericResult<IReadOnlyList<OptionRow>>.Failure(
                    FlowDataOptionsLog.TooManyRows(_logger, Name, _optionsDataStore, _optionsContainer, _maxRows));
            }

            var row = OptionRow.Of(record.Value);
            if (row.Reason is { } reason)
            {
                return GenericResult<IReadOnlyList<OptionRow>>.Failure(FlowDataOptionsLog.RowInvalid(_logger, Name, _optionsContainer, rows.Count + 1, reason));
            }

            rows.Add(row);
        }

        return GenericResult<IReadOnlyList<OptionRow>>.Success(rows);
    }

    // Applies the rows in the options' own order, each on the values the ones before it left, so the list is the same whatever order the store returned them in.
    private IGenericResult<IReadOnlyList<string>> Apply(IReadOnlyList<OptionRow> rows)
    {
        IReadOnlyList<string> values = [];
        foreach (var row in rows
            .OrderBy(row => row.OptionType, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => DataOptions.ByName(row.OptionType).ByName(row.Option).ApplyOrder)
            .ThenBy(row => row.Option, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.Value, StringComparer.Ordinal))
        {
            var applied = DataOptions.ByName(row.OptionType).ByName(row.Option)
                .Apply(new DataOptionContext(_dataStore, _container, _column, row.OptionType, row.Option, row.Value, row.IsActive, values, _logger));
            if (applied.IsFailure)
            {
                return FlowFailures.Caused<IReadOnlyList<string>>(
                    FlowDataOptionsLog.ApplyFailed(_logger, Name, row.OptionType, row.Option, _column, _container, FlowFailures.ReasonOf(applied)), applied);
            }

            if (applied.Value is not SelectListOutcome outcome)
            {
                return GenericResult<IReadOnlyList<string>>.Failure(FlowDataOptionsLog.OutcomeNotAList(_logger, Name, row.OptionType, row.Option));
            }

            values = outcome.Values;
        }

        return GenericResult<IReadOnlyList<string>>.Success(values);
    }

    // One stored row as the step reads it: the option type and option are required text, the value is text or none, and Active is a flag.
    private sealed record OptionRow(string OptionType, string Option, string? Value, bool IsActive, string? Reason)
    {
        internal static OptionRow Of(DataRecord record)
        {
            var fields = new Dictionary<string, object?>(record.FieldCount, StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < record.FieldCount; i++)
            {
                fields[record.Schema.GetFieldName(i)] = record[i];
            }

            return Invalid(fields, DataOptionRowFields.OptionType) is { } type ? Failed(type)
                : Invalid(fields, DataOptionRowFields.Option) is { } option ? Failed(option)
                : !TryFlag(fields, out var active) ? Failed($"{DataOptionRowFields.Active} is not a flag")
                : new OptionRow(
                    Convert.ToString(fields[DataOptionRowFields.OptionType], CultureInfo.InvariantCulture)!,
                    Convert.ToString(fields[DataOptionRowFields.Option], CultureInfo.InvariantCulture)!,
                    fields.TryGetValue(DataOptionRowFields.Value, out var value) && value is not null ? Convert.ToString(value, CultureInfo.InvariantCulture) : null,
                    active,
                    null);
        }

        private static OptionRow Failed(string reason) => new(string.Empty, string.Empty, null, false, reason);

        // Why the fields are checked and not assumed: a missing name could only be filled by guessing which option the row meant.
        private static string? Invalid(Dictionary<string, object?> fields, string field) =>
            fields.TryGetValue(field, out var value) && value is not null && !string.IsNullOrWhiteSpace(Convert.ToString(value, CultureInfo.InvariantCulture))
                ? null
                : $"{field} is missing";

        // A flag is a boolean, or a number a store keeps a flag in (zero is off); anything else is not a flag.
        private static bool TryFlag(Dictionary<string, object?> fields, out bool flag)
        {
            flag = false;
            if (!fields.TryGetValue(DataOptionRowFields.Active, out var value) || value is null)
            {
                return false;
            }

            if (value is bool boolean)
            {
                flag = boolean;
                return true;
            }

            if (Convert.GetTypeCode(value) is >= TypeCode.SByte and <= TypeCode.Decimal)
            {
                flag = Convert.ToDecimal(value, CultureInfo.InvariantCulture) != 0m;
                return true;
            }

            return false;
        }
    }
}
