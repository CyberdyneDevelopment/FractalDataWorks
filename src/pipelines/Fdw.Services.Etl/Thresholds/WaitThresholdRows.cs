using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Thresholds;

/// <summary>Reads the current rows of the configuration containers that hold thresholds on waiting runs, for the source that judges them and the store that writes them (FDW-3161).</summary>
internal sealed class WaitThresholdRows(ILogger logger)
{
    /// <summary>The configuration path of the containers that hold the levels, the thresholds and their rules.</summary>
    public const string PathName = "pipe";

    /// <summary>The container that holds the level each owner states.</summary>
    public const string LevelContainer = "WaitThresholdLevel";

    /// <summary>The container that holds the thresholds.</summary>
    public const string ThresholdContainer = "WaitThreshold";

    /// <summary>The container that holds the notification rule names of each threshold.</summary>
    public const string RuleContainer = "WaitThresholdRule";

    /// <summary>The configuration path of the schedules.</summary>
    public const string SchedulePath = "sched";

    /// <summary>The container that holds the schedules, by name.</summary>
    public const string ScheduleContainer = "Schedule";

    /// <summary>Reads the rows of a container that are current and not deleted.</summary>
    /// <typeparam name="T">The row type.</typeparam>
    /// <param name="gateway">The configuration gateway.</param>
    /// <param name="path">The configuration path.</param>
    /// <param name="container">The container.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The rows; a failure naming the container when they cannot be read.</returns>
    public async Task<IGenericResult<IReadOnlyList<T>>> Active<T>(IConfigurationGateway gateway, string path, string container, CancellationToken cancellationToken)
        where T : class
    {
        var result = await gateway
            .Execute<IEnumerable<T>>(
                new QueryCommand<T> { Filter = ActiveRows() }, new DataStoreTarget(EtlPipelineTypes.ConfigurationConnection, path, container), cancellationToken)
            .ConfigureAwait(false);
        return result.IsFailure || result.Value is null
            ? GenericResult<IReadOnlyList<T>>.Failure(WaitThresholdLog.RowsNotRead(logger, container, result.CurrentMessage))
            : GenericResult<IReadOnlyList<T>>.Success([.. result.Value]);
    }

    /// <summary>Whether a row belongs to an owner: the same kind and the same name (names are compared without regard to case).</summary>
    /// <param name="kind">The owner kind.</param>
    /// <param name="name">The owner name; none for the server.</param>
    /// <param name="rowKind">The owner kind the row states.</param>
    /// <param name="rowName">The owner name the row states.</param>
    /// <returns>True when the row belongs to the owner.</returns>
    public static bool Owns(string kind, string? name, string rowKind, string? rowName) =>
        string.Equals(kind, rowKind, StringComparison.Ordinal) && string.Equals(name, rowName, StringComparison.OrdinalIgnoreCase);

    /// <summary>Gets the target of one of the containers that hold thresholds.</summary>
    /// <param name="container">The container.</param>
    /// <returns>The target.</returns>
    public static DataStoreTarget Target(string container) => new(EtlPipelineTypes.ConfigurationConnection, PathName, container);

    /// <summary>Retires rows of a container, each by its own logical Id (a retire command takes the key values of one row).</summary>
    /// <param name="gateway">The configuration gateway.</param>
    /// <param name="container">The container.</param>
    /// <param name="ids">The logical Ids of the rows.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Success when every row is retired; the first failure otherwise.</returns>
    public static async Task<IGenericResult> Retire(IConfigurationGateway gateway, string container, List<Guid> ids, CancellationToken cancellationToken)
    {
        foreach (var id in ids)
        {
            var retired = await gateway.Execute<ConfigurationWriteResult>(
                new ConfigurationRetireCommand((KeyTypeBase)KeyTypes.ByName("Logical"), [id], markDeleted: true), Target(container), cancellationToken).ConfigureAwait(false);
            if (retired.IsFailure)
            {
                return retired;
            }
        }

        return GenericResult.Success();
    }

    /// <summary>Reads the current rows of the levels, the thresholds and the rules together.</summary>
    /// <param name="gateway">The configuration gateway.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The three tables; a failure naming the container that could not be read.</returns>
    public async Task<IGenericResult<WaitThresholdTables>> Tables(IConfigurationGateway gateway, CancellationToken cancellationToken)
    {
        var levels = await Active<WaitThresholdLevelRecord>(gateway, PathName, LevelContainer, cancellationToken).ConfigureAwait(false);
        if (levels.IsFailure || levels.Value is null)
        {
            return levels.ToNewResult<WaitThresholdTables>();
        }

        var thresholds = await Active<WaitThresholdRecord>(gateway, PathName, ThresholdContainer, cancellationToken).ConfigureAwait(false);
        if (thresholds.IsFailure || thresholds.Value is null)
        {
            return thresholds.ToNewResult<WaitThresholdTables>();
        }

        var rules = await Active<WaitThresholdRuleRecord>(gateway, PathName, RuleContainer, cancellationToken).ConfigureAwait(false);
        return rules.IsFailure || rules.Value is null
            ? rules.ToNewResult<WaitThresholdTables>()
            : GenericResult<WaitThresholdTables>.Success(new WaitThresholdTables(levels.Value, thresholds.Value, rules.Value));
    }

    private static FilterExpression ActiveRows()
    {
        var equal = FilterOperators.ByName("Equal");
        return new FilterExpression
        {
            Root = new FilterGroup
            {
                Operator = LogicalOperator.And,
                Nodes =
                [
                    new FilterCondition { PropertyName = "IsCurrent", Operator = equal, Value = true },
                    new FilterCondition { PropertyName = "IsDeleted", Operator = equal, Value = false },
                ],
            },
        };
    }
}
