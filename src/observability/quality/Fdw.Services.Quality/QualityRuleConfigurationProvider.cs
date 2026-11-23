using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Descriptions;
using Fdw.Services.Descriptions.Abstractions;
using Fdw.Services.Quality.Configuration;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Quality;

/// <summary>Supplies the QualityRule configuration.</summary>
/// <remarks>
/// A rule's <c>Description</c> and that of each of its reference values are not columns of the quality tables (FDW-6208): they are rows of the descriptions table under the
/// description types <c>QualityRule</c> and <c>QualityRuleReferenceValue</c> (owned by the rule), written in the unit of the rule's save, filled once for every set this provider
/// reads, and removed with the rule.
/// </remarks>
public sealed class QualityRuleConfigurationProvider
    : DomainConfigurationProviderBase<IQualityRuleImplementationConfiguration>,
      IQualityRuleConfigurationProvider,
      IConfigurationWriteObserver<IQualityRuleImplementationConfiguration>,
      IConfigurationReadObserver<IQualityRuleImplementationConfiguration>,
      IReferencedConfigurationProvider<IQualityRuleImplementationConfiguration>
{
    private const string RuleType = QualityRuleDescriptionType.OptionName;
    private const string ValueType = QualityRuleReferenceValueDescriptionType.OptionName;
    private const string DescriptionSubType = DescriptionTextSubType.OptionName;

    private readonly IDescriptionWriter _writer;
    private readonly IDescriptionReader _reader;

    /// <summary>Initializes a new instance of the <see cref="QualityRuleConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    /// <param name="pathName">The schema its rows live in -- passed by its registration.</param>
    /// <param name="containerName">The container its rows live in -- passed by its registration.</param>
    /// <param name="writer">Writes each rule's description, and each reference value's, in the unit of the rule's save (FDW-6208).</param>
    /// <param name="reader">Reads the descriptions of the rules a read has just composed, once for the set (FDW-6208).</param>
    public QualityRuleConfigurationProvider(
        ILogger<QualityRuleConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName,
        string containerName,
        IDescriptionWriter writer,
        IDescriptionReader reader)
        : base(logger, gatewayProvider, dataStoreName, pathName, containerName)
    {
        _writer = writer;
        _reader = reader;
    }

    /// <inheritdoc />
    /// <remarks>
    /// A rule saved with no text removes the one it had; nothing invents one. Each reference value is described by the Id the save gave it, owned by the rule, so a save that holds
    /// fewer values removes the descriptions of the ones it dropped.
    /// </remarks>
    public async Task<IGenericResult> Written(IQualityRuleImplementationConfiguration record, ConfigurationWriteScope unit, CancellationToken cancellationToken = default)
    {
        var described = await _writer.Describe(RuleType, DescriptionSubType, record.Id, record.Name, record.Description, unit, cancellationToken).ConfigureAwait(false);
        return described.IsFailure
            ? described
            : await _writer.DescribeOwned(
                ValueType, DescriptionSubType, new DescriptionOwner(record.Id, record.Name),
                [.. record.ReferenceValues.Select(value => new OwnedDescription(value.Id, value.Name, value.Description))], unit, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    /// <remarks>A rule or a value with no description row keeps an empty description; no other language's text and no column stands in for it.</remarks>
    public async Task<IGenericResult> Read(IReadOnlyList<IQualityRuleImplementationConfiguration> records, DateTimeOffset? asOf, CancellationToken cancellationToken = default)
    {
        var rules = await _reader.Fill(RuleType, DescriptionSubType, records, rule => rule.Id, (rule, text) => rule.Description = text, cancellationToken).ConfigureAwait(false);
        if (rules.IsFailure)
            return rules;

        // The reference values' texts are read only for the rules whose records hold values: a record-only read carries none, and a read of their texts would fetch rows nothing receives.
        var owners = records.Where(rule => rule.ReferenceValues.Count > 0).Select(rule => rule.Id).ToList();
        if (owners.Count == 0)
            return GenericResult.Success();

        var texts = await _reader.ReadOwnedBy(ValueType, DescriptionSubType, owners, cancellationToken).ConfigureAwait(false);
        if (texts.IsFailure || texts.Value is not { } held)
            return texts;

        foreach (var value in records.SelectMany(rule => rule.ReferenceValues))
        {
            if (held.TryGetValue(value.Id, out var text))
                value.Description = text.Text;
        }

        return GenericResult.Success();
    }

    /// <inheritdoc />
    /// <remarks>A rule's descriptions and those of its reference values name it, so they go with it, in every language, before it is retired.</remarks>
    public async Task<IGenericResult> RemoveReferences(Guid memberId, ConfigurationWriteScope unit, CancellationToken cancellationToken = default)
    {
        var about = await _writer.DeleteAbout(RuleType, memberId, unit, cancellationToken).ConfigureAwait(false);
        return about.IsFailure
            ? about
            : await _writer.DeleteOwnedBy(ValueType, memberId, unit, cancellationToken).ConfigureAwait(false);
    }
}
