using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Data.Abstractions;
using Fdw.Operations.Abstractions.Thresholds;
using Fdw.Operations.Configuration;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Operations.Thresholds;

/// <summary>
/// Finds the thresholds attached to a subject (FDW-3936): reads the current links in the link table of the subject's kind, then each threshold through its configuration
/// provider, in the scope of the caller's tenant, and refuses one that cannot be judged.
/// </summary>
public sealed class ThresholdSource : IThresholdSource
{
    private readonly IConfigurationGatewayProvider _gateways;
    private readonly IThresholdConfigurationProvider _thresholds;
    private readonly string _dataStoreName;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="ThresholdSource"/> class.</summary>
    /// <param name="gateways">Supplies the gateway onto the configuration store, for the link tables.</param>
    /// <param name="thresholds">Reads a threshold by Id.</param>
    /// <param name="dataStoreName">The configuration data store.</param>
    /// <param name="logger">The logger.</param>
    public ThresholdSource(IConfigurationGatewayProvider gateways, IThresholdConfigurationProvider thresholds, string dataStoreName, ILogger<ThresholdSource>? logger = null)
    {
        _gateways = gateways;
        _thresholds = thresholds;
        _dataStoreName = dataStoreName;
        _logger = logger ?? NullLogger<ThresholdSource>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<IThresholdDefinition>>> For(ThresholdSubject subject, CancellationToken cancellationToken = default)
    {
        var kind = ThresholdSubjectKinds.ByName(subject.Kind);
        if (ReferenceEquals(kind, ThresholdSubjectKinds.NotFound))
        {
            return ThresholdRuleFailures.KindUnknown(subject.Kind).ToNewResult<IReadOnlyList<IThresholdDefinition>>();
        }

        var links = await Links(kind, subject, cancellationToken).ConfigureAwait(false);
        if (links.IsFailure || links.Value is null)
        {
            return links.ToNewResult<IReadOnlyList<IThresholdDefinition>>();
        }

        var definitions = new List<IThresholdDefinition>();
        foreach (var link in links.Value)
        {
            var definition = await Definition(link, subject, cancellationToken).ConfigureAwait(false);
            if (definition.IsFailure || definition.Value is null)
            {
                return definition.ToNewResult<IReadOnlyList<IThresholdDefinition>>();
            }

            definitions.Add(definition.Value);
        }

        ThresholdLog.ThresholdsLoaded(_logger, subject.Kind, subject.Name, definitions.Count);
        return GenericResult<IReadOnlyList<IThresholdDefinition>>.Success(definitions);
    }

    private async Task<IGenericResult<IReadOnlyList<ThresholdLinkConfiguration>>> Links(IThresholdSubjectKind kind, ThresholdSubject subject, CancellationToken cancellationToken)
    {
        var gateway = _gateways.Get(_dataStoreName);
        if (gateway.IsFailure || gateway.Value is not { } connection)
        {
            return gateway.ToNewResult<IReadOnlyList<ThresholdLinkConfiguration>>();
        }

        var command = new QueryCommandBuilder<ThresholdLinkConfiguration>(_dataStoreName, kind.PathName, kind.ContainerName)
            .Where("SubjectId", subject.SubjectId)
            .Where("IsCurrent", true)
            .Where("IsDeleted", false)
            .Build().Command;
        var rows = await connection.Execute<IEnumerable<ThresholdLinkConfiguration>>(command, new DataStoreTarget(_dataStoreName, kind.PathName, kind.ContainerName), cancellationToken).ConfigureAwait(false);
        return rows.IsSuccess
            ? GenericResult<IReadOnlyList<ThresholdLinkConfiguration>>.Success([.. rows.Value ?? []])
            : rows.ToNewResult<IReadOnlyList<ThresholdLinkConfiguration>>();
    }

    private async Task<IGenericResult<IThresholdDefinition>> Definition(ThresholdLinkConfiguration link, ThresholdSubject subject, CancellationToken cancellationToken)
    {
        var stored = await _thresholds.Get(link.ThresholdId, cancellationToken).ConfigureAwait(false);
        if (stored.IsFailure)
        {
            return stored.ToNewResult<IThresholdDefinition>();
        }

        // Why a link to a threshold that is not there is a failure: a read that finds nothing is success with no value, and a list built from it would carry no value either,
        // so the subject would be judged against thresholds that were never read. The link names a threshold, and the threshold is not there.
        if (stored.Value is null)
        {
            return ThresholdRuleFailures.NotFound(link.ThresholdId.ToString()).ToNewResult<IThresholdDefinition>();
        }

        var definition = ThresholdDefinitions.From(stored.Value);
        if (definition.IsFailure || definition.Value is null)
        {
            return definition;
        }

        return string.Equals(definition.Value.Type.SubjectKind, subject.Kind, StringComparison.Ordinal)
            ? definition
            : ThresholdRuleFailures.KindMismatch(stored.Value.Name, definition.Value.Type.SubjectKind, subject.Kind).ToNewResult<IThresholdDefinition>();
    }
}
