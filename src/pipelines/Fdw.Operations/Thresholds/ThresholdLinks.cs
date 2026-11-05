using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Operations.Abstractions.Thresholds;
using Fdw.Operations.Configuration;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Operations.Thresholds;

/// <summary>
/// Attaches thresholds to subjects (FDW-3948): one row in the link table of the subject's kind, holding the subject's Id and the threshold's Id. A threshold attaches to a subject
/// once, and a detach retires the row. Nothing is assumed: a threshold that does not exist, or judges another kind of subject, is refused.
/// </summary>
public sealed class ThresholdLinks : IThresholdLinks
{
    private readonly IConfigurationGatewayProvider _gateways;
    private readonly IThresholdConfigurationProvider _thresholds;
    private readonly string _dataStoreName;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="ThresholdLinks"/> class.</summary>
    /// <param name="gateways">Supplies the gateway onto the configuration store, for the link tables.</param>
    /// <param name="thresholds">Reads a threshold by name or Id.</param>
    /// <param name="dataStoreName">The configuration data store.</param>
    /// <param name="logger">The logger.</param>
    public ThresholdLinks(IConfigurationGatewayProvider gateways, IThresholdConfigurationProvider thresholds, string dataStoreName, ILogger<ThresholdLinks>? logger = null)
    {
        _gateways = gateways ?? throw new ArgumentNullException(nameof(gateways));
        _thresholds = thresholds ?? throw new ArgumentNullException(nameof(thresholds));
        _dataStoreName = dataStoreName ?? throw new ArgumentNullException(nameof(dataStoreName));
        _logger = logger ?? NullLogger<ThresholdLinks>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Attach(string subjectKind, Guid subjectId, string thresholdName, CancellationToken cancellationToken = default)
    {
        var target = await Resolve(subjectKind, thresholdName, cancellationToken).ConfigureAwait(false);
        if (target.IsFailure || target.Value is not { } found)
        {
            return target;
        }

        var current = await Links(found.Kind, found.Gateway, subjectId, cancellationToken).ConfigureAwait(false);
        if (current.IsFailure || current.Value is null)
        {
            return current;
        }

        if (current.Value.Any(link => link.ThresholdId == found.Threshold.Id))
        {
            return ThresholdRuleFailures.LinkExists(thresholdName, subjectKind, subjectId);
        }

        var link = new ThresholdLinkConfiguration { Id = Guid.CreateVersion7(), SubjectId = subjectId, ThresholdId = found.Threshold.Id, CreateDate = DateTimeOffset.UtcNow };
        var inserted = await found.Gateway.Execute<ConfigurationWriteResult>(
            new ConfigurationSaveCommand<ThresholdLinkConfiguration>(link, replacesCurrent: false), Target(found.Kind), cancellationToken).ConfigureAwait(false);
        if (inserted.IsSuccess)
        {
            ThresholdLog.ThresholdAttached(_logger, thresholdName, subjectKind, subjectId);
        }

        return inserted;
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Detach(string subjectKind, Guid subjectId, string thresholdName, CancellationToken cancellationToken = default)
    {
        var target = await Resolve(subjectKind, thresholdName, cancellationToken).ConfigureAwait(false);
        if (target.IsFailure || target.Value is not { } found)
        {
            return target;
        }

        var current = await Links(found.Kind, found.Gateway, subjectId, cancellationToken).ConfigureAwait(false);
        if (current.IsFailure || current.Value is null)
        {
            return current;
        }

        var held = current.Value.FirstOrDefault(link => link.ThresholdId == found.Threshold.Id);
        if (held is null)
        {
            return ThresholdRuleFailures.LinkAbsent(thresholdName, subjectKind, subjectId);
        }

        var retired = await found.Gateway.Execute<ConfigurationWriteResult>(
            new ConfigurationRetireCommand((KeyTypeBase)KeyTypes.ByName("Logical"), [held.Id], markDeleted: true), Target(found.Kind), cancellationToken).ConfigureAwait(false);
        if (retired.IsSuccess)
        {
            ThresholdLog.ThresholdDetached(_logger, thresholdName, subjectKind, subjectId);
        }

        return retired;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<IThresholdImplementationConfiguration>>> ThresholdsOf(string subjectKind, Guid subjectId, CancellationToken cancellationToken = default)
    {
        var kind = ThresholdSubjectKinds.ByName(subjectKind);
        if (ReferenceEquals(kind, ThresholdSubjectKinds.NotFound))
        {
            return ThresholdRuleFailures.KindUnknown(subjectKind).ToNewResult<IReadOnlyList<IThresholdImplementationConfiguration>>();
        }

        var gateway = _gateways.Get(_dataStoreName);
        if (gateway.IsFailure || gateway.Value is not { } connection)
        {
            return gateway.ToNewResult<IReadOnlyList<IThresholdImplementationConfiguration>>();
        }

        var links = await Links(kind, connection, subjectId, cancellationToken).ConfigureAwait(false);
        if (links.IsFailure || links.Value is null)
        {
            return links.ToNewResult<IReadOnlyList<IThresholdImplementationConfiguration>>();
        }

        var found = new List<IThresholdImplementationConfiguration>();
        foreach (var link in links.Value)
        {
            var stored = await _thresholds.Get(link.ThresholdId, cancellationToken).ConfigureAwait(false);
            if (stored.IsFailure || stored.Value is null)
            {
                return stored.IsFailure ? stored.ToNewResult<IReadOnlyList<IThresholdImplementationConfiguration>>() : ThresholdRuleFailures.NotFound(link.ThresholdId.ToString("D")).ToNewResult<IReadOnlyList<IThresholdImplementationConfiguration>>();
            }

            found.Add(stored.Value);
        }

        return GenericResult<IReadOnlyList<IThresholdImplementationConfiguration>>.Success(found);
    }

    private static DataStoreTarget Target(IThresholdSubjectKind kind, string dataStoreName) => new(dataStoreName, kind.PathName, kind.ContainerName);

    private DataStoreTarget Target(IThresholdSubjectKind kind) => Target(kind, _dataStoreName);

    private async Task<IGenericResult<ThresholdLinkTarget>> Resolve(
        string subjectKind, string thresholdName, CancellationToken cancellationToken)
    {
        var kind = ThresholdSubjectKinds.ByName(subjectKind);
        if (ReferenceEquals(kind, ThresholdSubjectKinds.NotFound))
        {
            return ThresholdRuleFailures.KindUnknown(subjectKind).ToNewResult<ThresholdLinkTarget>();
        }

        var stored = await _thresholds.Get(thresholdName, cancellationToken).ConfigureAwait(false);
        if (stored.IsFailure || stored.Value is not { } threshold)
        {
            return stored.IsFailure
                ? stored.ToNewResult<ThresholdLinkTarget>()
                : ThresholdRuleFailures.NotFound(thresholdName).ToNewResult<ThresholdLinkTarget>();
        }

        if (!string.Equals(threshold.SubjectKind, kind.Name, StringComparison.Ordinal))
        {
            return ThresholdRuleFailures.KindMismatch(thresholdName, threshold.SubjectKind, kind.Name).ToNewResult<ThresholdLinkTarget>();
        }

        var gateway = _gateways.Get(_dataStoreName);
        return gateway.IsFailure || gateway.Value is not { } connection
            ? gateway.ToNewResult<ThresholdLinkTarget>()
            : GenericResult<ThresholdLinkTarget>.Success(new ThresholdLinkTarget(kind, connection, threshold));
    }

    private async Task<IGenericResult<IReadOnlyList<ThresholdLinkConfiguration>>> Links(IThresholdSubjectKind kind, IConfigurationGateway gateway, Guid subjectId, CancellationToken cancellationToken)
    {
        var command = new QueryCommandBuilder<ThresholdLinkConfiguration>(_dataStoreName, kind.PathName, kind.ContainerName)
            .Where("SubjectId", subjectId)
            .Where("IsCurrent", true)
            .Where("IsDeleted", false)
            .Build().Command;
        var rows = await gateway.Execute<IEnumerable<ThresholdLinkConfiguration>>(command, Target(kind), cancellationToken).ConfigureAwait(false);
        return rows.IsSuccess
            ? GenericResult<IReadOnlyList<ThresholdLinkConfiguration>>.Success([.. rows.Value ?? []])
            : rows.ToNewResult<IReadOnlyList<ThresholdLinkConfiguration>>();
    }
}
