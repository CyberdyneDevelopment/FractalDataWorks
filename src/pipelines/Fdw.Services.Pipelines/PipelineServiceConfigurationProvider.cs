using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Descriptions;
using Fdw.Services.Descriptions.Abstractions;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Services.Pipelines.Commands;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines;

/// <summary>Supplies the Pipeline configuration.</summary>
/// <remarks>
/// A pipeline's <c>Description</c> and its parameters' are not columns of the pipeline tables (FDW-7323, a group of FDW-6208): they are rows of the descriptions table under the
/// description types <c>Pipeline</c> and <c>PipelineParameter</c>, written in the unit of the save, filled once for every set of composed members, and removed with the pipeline.
/// The text is kept under the Id of the domain row, which the provider stamps onto the implementation; a parameter's under its own Id, owned by the pipeline.
/// </remarks>
public sealed class PipelineServiceConfigurationProvider
    : DomainConfigurationProviderBase<IPipelineImplementationConfiguration>,
      IPipelineConfigurationProvider,
      IPlacedConfigurationProvider<IPipelineImplementationConfiguration>,
      IConfigurationWriteObserver<IPipelineImplementationConfiguration>,
      IConfigurationReadObserver<IPipelineImplementationConfiguration>,
      IReferencedConfigurationProvider<IPipelineImplementationConfiguration>
{
    private const string DescriptionType = PipelineDescriptionType.OptionName;
    private const string ParameterType = PipelineParameterDescriptionType.OptionName;
    private const string DescriptionSubType = DescriptionTextSubType.OptionName;

    private readonly IDescriptionWriter _writer;
    private readonly IDescriptionReader _reader;

    /// <summary>Initializes a new instance of the <see cref="PipelineServiceConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    /// <param name="pathName">The schema its rows live in -- passed by its registration.</param>
    /// <param name="containerName">The container its rows live in -- passed by its registration.</param>
    /// <param name="placement">Places each member this provider makes, in the unit of work that makes it (FDW-3692).</param>
    /// <param name="writer">Writes each pipeline's description, and its parameters', in the unit of its save (FDW-7323).</param>
    /// <param name="reader">Reads the descriptions of the pipelines a read has just composed, once for the set (FDW-7323).</param>
    public PipelineServiceConfigurationProvider(
        ILogger<PipelineServiceConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName,
        string containerName,
        IConfigurationPlacement placement,
        IDescriptionWriter writer,
        IDescriptionReader reader)
        : base(logger, gatewayProvider, dataStoreName, pathName, containerName)
    {
        Placement = placement;
        _writer = writer;
        _reader = reader;
    }

    /// <inheritdoc />
    public IConfigurationPlacement Placement { get; }

    /// <inheritdoc />
    public string PlacementKind => "Pipeline";

    /// <inheritdoc />
    public bool Needs(IPipelineImplementationConfiguration record) => true;

    /// <inheritdoc />
    /// <remarks>
    /// A pipeline saved with no text removes the one it had; nothing invents one. Its parameters are kept in step with one <see cref="IDescriptionWriter.DescribeOwned"/>: one
    /// read of what the pipeline holds, a write only for what the save changed, and a removal for a parameter the pipeline no longer holds or whose text is now empty.
    /// </remarks>
    public async Task<IGenericResult> Written(IPipelineImplementationConfiguration record, ConfigurationWriteScope unit, CancellationToken cancellationToken = default)
    {
        var own = await _writer.Describe(DescriptionType, DescriptionSubType, record.Id, record.Name, record.Description, unit, cancellationToken).ConfigureAwait(false);
        return own.IsFailure
            ? own
            : await _writer.DescribeOwned(
                ParameterType, DescriptionSubType, new DescriptionOwner(record.Id, record.Name),
                [.. record.DescribedParameters.Select(parameter => new OwnedDescription(parameter.Id, parameter.Name, parameter.Description))], unit, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    /// <remarks>
    /// A pipeline with no description row keeps an empty description; no other language's text and no column stands in for it. The parameters a read composed are filled with
    /// one read for the whole set; a read that composed none costs none.
    /// </remarks>
    public async Task<IGenericResult> Read(IReadOnlyList<IPipelineImplementationConfiguration> records, DateTimeOffset? asOf, CancellationToken cancellationToken = default)
    {
        var own = await _reader.Fill(DescriptionType, DescriptionSubType, records, member => member.Id, (member, text) => member.Description = text, cancellationToken).ConfigureAwait(false);
        if (own.IsFailure) return own;

        var parameters = records.SelectMany(record => record.DescribedParameters).ToList();
        if (parameters.Count == 0) return GenericResult.Success();

        var texts = await _reader.ReadOwnedBy(ParameterType, DescriptionSubType, [.. records.Select(record => record.Id)], cancellationToken).ConfigureAwait(false);
        if (texts.IsFailure || texts.Value is not { } held) return texts;

        foreach (var parameter in parameters)
        {
            if (held.TryGetValue(parameter.Id, out var text)) parameter.Description = text.Text;
        }

        return GenericResult.Success();
    }

    /// <inheritdoc />
    /// <remarks>A pipeline's descriptions, and its parameters', name it, so they go with it, in every language, before it is retired.</remarks>
    public async Task<IGenericResult> RemoveReferences(Guid memberId, ConfigurationWriteScope unit, CancellationToken cancellationToken = default)
    {
        var own = await _writer.DeleteAbout(DescriptionType, memberId, unit, cancellationToken).ConfigureAwait(false);
        return own.IsFailure
            ? own
            : await _writer.DeleteOwnedBy(ParameterType, memberId, unit, cancellationToken).ConfigureAwait(false);
    }
}
