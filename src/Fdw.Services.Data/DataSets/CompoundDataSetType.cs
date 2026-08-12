using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Execution;
using Fdw.Services.Data.Logging;
using Fdw.Services.Data.Results;
using IDataField = Fdw.Data.DataSets.Abstractions.IDataField;

namespace Fdw.Services.Data;

/// <summary>
/// Compound dataset strategy: sources within a SINGLE store (a database) whose join is PUSHED DOWN
/// to the store as one query.
/// </summary>
/// <remarks>
/// Why: registered as the <c>"Compound"</c> member of <see cref="DataSetTypes"/>; selected when a
/// dataset's authored <c>ServiceOptionType</c> is <c>"Compound"</c>. The defining property is that the
/// join is performed by the backend (not in memory), so all sources must live in one store.
/// <para>
/// Builds a single <see cref="QueryCommand{T}"/> carrying JOIN expressions and a multi-source
/// projection (each field qualified with its owning container name). The regular QueryTranslator
/// renders this into a pushed-down JOIN SELECT statement. No in-memory join. No fallback.
/// </para>
/// </remarks>
[TypeOption(typeof(DataSetTypes), "Compound")]
public sealed class CompoundDataSetType : DataSetTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="CompoundDataSetType"/> class.</summary>
    public CompoundDataSetType()
        : base(2, "Compound", "Single-store dataset whose join is pushed down to the store",
            typeof(object), Array.Empty<IDataField>(), category: "DataSetStrategy")
    {
    }

    /// <inheritdoc />
    public override IDataQuery CreateQuery() => new DataQueryBuilder<object>(Name);

    /// <inheritdoc />
    public override async Task<IGenericResult<T>> Execute<T>(
        IDataSetExecutionContext context, IDataCommand command, CancellationToken ct = default)
    {
        if (context is not DataSetExecutionContext ctx)
            return GenericResult<T>.Failure(DataServiceResultCodes.ByName("DataSetConfigurationRequired"));

        var sources = ctx.Config.Sources;
        if (sources is null || sources.Count == 0)
            return GenericResult<T>.Failure(DataGatewayLogger.DataSetNoSources(ctx.Logger, ctx.Config.Name));

        // Why: a Compound join is pushed down to a single store; sources spanning stores cannot be
        // pushed down (that is a Federated dataset). Fail loud.
        var storeCount = sources.Select(s => s.DataStoreName).Distinct(StringComparer.OrdinalIgnoreCase).Count();
        if (storeCount > 1)
            return GenericResult<T>.Failure(
                DataGatewayLogger.CompoundSourcesSpanStores(ctx.Logger, ctx.Config.Name, storeCount));

        DataGatewayLogger.ExecutingCompoundDataSet(ctx.Logger, ctx.Config.Name, sources.Count);

        // Primary source is the FROM table: lowest Priority wins (ties keep declaration order).
        var primarySource = sources.OrderBy(s => s.Priority).First();
        var primaryContainerName = DataSetExecutionHelpers.GetContainerName(primarySource);
        if (primaryContainerName == null)
            return GenericResult<T>.Failure(DataGatewayLogger.SourceNoContainer(ctx.Logger, primarySource.SourceName, ctx.Config.Name));

        var joinResult = BuildJoinExpressions<T>(ctx, sources);
        if (joinResult.HasError) return joinResult.ErrorResult!;

        var projResult = BuildProjection<T>(ctx, sources, primarySource);
        if (projResult.HasError) return projResult.ErrorResult!;

        var filterResult = TranslateCompoundFilter<T>(ctx, command, primarySource);
        if (filterResult.HasError) return filterResult.ErrorResult!;

        return await ExecuteCompoundQuery<T>(
            ctx, command, primarySource, primaryContainerName,
            joinResult.Joins!, projResult.Projection, filterResult.TranslatedFilter, ct)
            .ConfigureAwait(false);
    }

    private static (bool HasError, List<IJoinExpression>? Joins, IGenericResult<T>? ErrorResult) BuildJoinExpressions<T>(
        DataSetExecutionContext ctx,
        IList<DataSetSourceConfiguration> sources)
    {
        var sourceLookup = sources.ToDictionary(s => s.SourceName, StringComparer.OrdinalIgnoreCase);
        var joinExpressions = new List<IJoinExpression>(ctx.Config.Joins.Count);

        foreach (var join in ctx.Config.Joins)
        {
            if (!sourceLookup.TryGetValue(join.LeftSource, out var leftSource))
                return (true, null, GenericResult<T>.Failure(
                    DataGatewayLogger.JoinSourceNotFound(ctx.Logger, ctx.Config.Name, join.LeftSource)));

            if (!sourceLookup.TryGetValue(join.RightSource, out var rightSource))
                return (true, null, GenericResult<T>.Failure(
                    DataGatewayLogger.JoinSourceNotFound(ctx.Logger, ctx.Config.Name, join.RightSource)));

            var rightContainerName = DataSetExecutionHelpers.GetContainerName(rightSource);
            if (rightContainerName == null)
                return (true, null, GenericResult<T>.Failure(
                    DataGatewayLogger.SourceNoContainer(ctx.Logger, rightSource.SourceName, ctx.Config.Name)));

            // Why: FieldMappings key=logical, value=physical. Use physical column in JOIN ON clause.
            // If no mapping exists for the join field, use the logical name as the physical name (one-to-one column).
            joinExpressions.Add(new JoinExpression
            {
                TargetContainerName = rightContainerName,
                JoinType = join.JoinType,
                JoinConditions =
                [
                    (leftSource.FieldMappings.TryGetValue(join.LeftField, out var lp) ? lp : join.LeftField,
                     rightSource.FieldMappings.TryGetValue(join.RightField, out var rp) ? rp : join.RightField),
                ],
            });
        }

        return (false, joinExpressions, null);
    }

    private static (bool HasError, IProjectionExpression? Projection, IGenericResult<T>? ErrorResult) BuildProjection<T>(
        DataSetExecutionContext ctx,
        IList<DataSetSourceConfiguration> sources,
        DataSetSourceConfiguration primarySource)
    {
        // Calculated fields are excluded from the SQL projection and applied post-execution.
        var nonCalculatedFields = ctx.Config.Fields.Where(f => !f.IsCalculated).ToList();
        if (nonCalculatedFields.Count == 0)
            return (false, null, null);

        var projectionFields = new List<ProjectionField>(nonCalculatedFields.Count);
        foreach (var field in nonCalculatedFields)
        {
            // Find which source owns this logical field via FieldMappings (key=logical, value=physical).
            DataSetSourceConfiguration? owningSource = null;
            string? physicalColumn = null;

            foreach (var source in sources)
            {
                if (source.FieldMappings.TryGetValue(field.Name, out var phys))
                {
                    owningSource = source;
                    physicalColumn = phys;
                    break;
                }
            }

            // Why: unmapped fields fall through to the primary source using the logical name as physical.
            // This is the common case when a column name matches the field name directly.
            if (owningSource == null)
            {
                owningSource = primarySource;
                physicalColumn = field.Name;
            }

            var sourceContainerName = DataSetExecutionHelpers.GetContainerName(owningSource);
            if (sourceContainerName == null)
                return (true, null, GenericResult<T>.Failure(
                    DataGatewayLogger.SourceNoContainer(ctx.Logger, owningSource.SourceName, ctx.Config.Name)));

            projectionFields.Add(new ProjectionField
            {
                PropertyName = physicalColumn!,
                // Why: Alias = logical name so result rows carry logical names; no post-query rename needed.
                Alias = field.Name,
                SourceContainer = sourceContainerName,
            });
        }

        return projectionFields.Count > 0
            ? (false, new ProjectionExpression { Fields = projectionFields }, null)
            : (false, null, null);
    }

    private static (bool HasError, IFilterExpression? TranslatedFilter, IGenericResult<T>? ErrorResult) TranslateCompoundFilter<T>(
        DataSetExecutionContext ctx,
        IDataCommand command,
        DataSetSourceConfiguration primarySource)
    {
        if (command is not QueryCommand<T> { Filter: not null } queryCommand)
            return (false, null, null);

        // Why: use primary source FieldMappings for filter translation. Compound datasets with
        // cross-source filter conditions must author the field in the primary source's FieldMappings
        // or use unambiguous physical names already.
        var fieldMappings = primarySource.FieldMappings.Count > 0 ? primarySource.FieldMappings : null;
        var filterResult = ctx.Pushdown.TranslateToPhysical(queryCommand.Filter, fieldMappings);
        if (!filterResult.IsSuccess)
            return (true, null, GenericResult<T>.Failure(
                DataGatewayLogger.FilterTranslationFailed(ctx.Logger, primarySource.SourceName, filterResult.CurrentMessage ?? "Unknown error")));

        return (false, filterResult.Value, null);
    }

    private static async Task<IGenericResult<T>> ExecuteCompoundQuery<T>(
        DataSetExecutionContext ctx,
        IDataCommand command,
        DataSetSourceConfiguration primarySource,
        string primaryContainerName,
        List<IJoinExpression> joinExpressions,
        IProjectionExpression? projection,
        IFilterExpression? translatedFilter,
        CancellationToken ct)
    {
        var sourceQuery = new QueryCommand<T>
        {
            Joins = joinExpressions,
            Projection = projection,
            Filter = translatedFilter,
            Ordering = (command as QueryCommand<T>)?.Ordering,
            Paging = (command as QueryCommand<T>)?.Paging,
        };

        // Resolve primary container from the DataStore provider.
        var containerResult = await ctx.DataStoreProvider
            .Get(primarySource.DataStoreName, primarySource.PathValue, primaryContainerName, ct)
            .ConfigureAwait(false);
        if (!containerResult.IsSuccess || containerResult.Value == null)
            return GenericResult<T>.Failure(DataGatewayLogger.SourceContainerBuildFailed(ctx.Logger, primarySource.SourceName));

        // Resolve the connection for the primary source.
        var connectionResult = await ctx.ConnectionProvider
            .Get<IDataConnection>(primarySource.ConnectionName, ct)
            .ConfigureAwait(false);
        if (!connectionResult.IsSuccess || connectionResult.Value == null)
            return GenericResult<T>.Failure(
                DataGatewayLogger.ConnectionRetrievalFailed(ctx.Logger, primarySource.ConnectionName, connectionResult.CurrentMessage ?? "Unknown error"));

        // Execute the pushed-down compound query through the single connection.
        var result = await connectionResult.Value.Execute<T>(sourceQuery, containerResult.Value, ct).ConfigureAwait(false);

        // Apply calculated fields post-execution (they operate on logical field names from SQL aliases).
        var calculatedFields = ctx.Config.Fields.Where(f => f.IsCalculated).ToList();
        if (calculatedFields.Count > 0 && result.IsSuccess && result.Value != null)
        {
            DataGatewayLogger.ApplyingCalculatedFields(ctx.Logger, calculatedFields.Count, ctx.Config.Name);
            result = DataSetExecutionHelpers.ApplyCalculatedFields(ctx, result, calculatedFields, ctx.Config.Name);
        }

        return result;
    }
}
