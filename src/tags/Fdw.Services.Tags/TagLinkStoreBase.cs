using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using Fdw.Collections;
using Fdw.Commands.Data.Abstractions;
using Fdw.Commands.Data;
using Fdw.Configuration;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Tags.Abstractions;

namespace Fdw.Services.Tags;

/// <summary>Base for the stores that read and write the link rows of a kind. One per kind, found by the kind's name.</summary>
/// <remarks>
/// A kind's option carries data only, and a link row is a typed record per table, so the type-specific part lives here, behind a
/// non-generic surface the service can call without knowing the record type.
/// </remarks>
public abstract class TagLinkStoreBase : TypeOptionBase<TagLinkStoreBase>, ITagLinkStore
{
    /// <summary>Initializes a new instance of the <see cref="TagLinkStoreBase"/> class.</summary>
    /// <param name="name">The name of the kind this store serves.</param>
    protected TagLinkStoreBase(string name)
        : base(name)
    {
    }

    /// <summary>Reads the current links of one thing.</summary>
    /// <param name="gateway">The gateway onto the configuration store.</param>
    /// <param name="kind">The kind, which names the table and its thing-id column.</param>
    /// <param name="dataStoreName">The configuration connection.</param>
    /// <param name="thingId">The thing.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The thing's current links.</returns>
    public abstract Task<IGenericResult<IReadOnlyList<TagLinkRow>>> ForThing(
        IConfigurationGateway gateway, ITaggableKind kind, string dataStoreName, Guid thingId, CancellationToken cancellationToken = default);

    /// <summary>Reads the current links of one tag.</summary>
    /// <param name="gateway">The gateway onto the configuration store.</param>
    /// <param name="kind">The kind, which names the table.</param>
    /// <param name="dataStoreName">The configuration connection.</param>
    /// <param name="tagId">The tag.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The tag's current links.</returns>
    public abstract Task<IGenericResult<IReadOnlyList<TagLinkRow>>> ForTag(
        IConfigurationGateway gateway, ITaggableKind kind, string dataStoreName, Guid tagId, CancellationToken cancellationToken = default);

    /// <summary>Reads the current links of every thing of the kind, in one query.</summary>
    /// <param name="gateway">The gateway onto the configuration store.</param>
    /// <param name="kind">The kind, which names the table.</param>
    /// <param name="dataStoreName">The configuration connection.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Every current link of the kind.</returns>
    public abstract Task<IGenericResult<IReadOnlyList<TagLinkRow>>> ForKind(
        IConfigurationGateway gateway, ITaggableKind kind, string dataStoreName, CancellationToken cancellationToken = default);

    /// <summary>Inserts a link; nothing is retired.</summary>
    /// <param name="gateway">The gateway onto the configuration store.</param>
    /// <param name="kind">The kind, which names the table.</param>
    /// <param name="dataStoreName">The configuration connection.</param>
    /// <param name="thingId">The thing that takes the tag.</param>
    /// <param name="tagId">The tag.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The write result.</returns>
    public abstract Task<IGenericResult<ConfigurationWriteResult>> Insert(
        IConfigurationGateway gateway, ITaggableKind kind, string dataStoreName, Guid thingId, Guid tagId, CancellationToken cancellationToken = default);

    /// <summary>Retires a link by its own Id.</summary>
    /// <param name="gateway">The gateway onto the configuration store.</param>
    /// <param name="kind">The kind, which names the table.</param>
    /// <param name="dataStoreName">The configuration connection.</param>
    /// <param name="linkId">The link's own Id.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The write result.</returns>
    public abstract Task<IGenericResult<ConfigurationWriteResult>> Retire(
        IConfigurationGateway gateway, ITaggableKind kind, string dataStoreName, Guid linkId, CancellationToken cancellationToken = default);
}

/// <summary>The part of a link store that depends on the record type of its table.</summary>
/// <typeparam name="TLink">The record class of the link table.</typeparam>
public abstract class TagLinkStoreBase<TLink> : TagLinkStoreBase
    where TLink : class, IGenericConfiguration
{
    /// <summary>Initializes a new instance of the <see cref="TagLinkStoreBase{TLink}"/> class.</summary>
    /// <param name="name">The name of the kind this store serves.</param>
    protected TagLinkStoreBase(string name)
        : base(name)
    {
    }

    /// <summary>Makes the record for a new link, with its Id and creation time stamped.</summary>
    /// <param name="thingId">The thing that takes the tag.</param>
    /// <param name="tagId">The tag.</param>
    /// <returns>The record to insert.</returns>
    protected abstract TLink Make(Guid thingId, Guid tagId);

    /// <summary>Reads the three Ids of a record.</summary>
    /// <param name="link">The record.</param>
    /// <returns>The row.</returns>
    protected abstract TagLinkRow Row(TLink link);

    /// <inheritdoc />
    public override Task<IGenericResult<IReadOnlyList<TagLinkRow>>> ForThing(
        IConfigurationGateway gateway, ITaggableKind kind, string dataStoreName, Guid thingId, CancellationToken cancellationToken = default)
        => Read(gateway, kind, dataStoreName, kind.ThingIdColumn, thingId, cancellationToken);

    /// <inheritdoc />
    public override Task<IGenericResult<IReadOnlyList<TagLinkRow>>> ForTag(
        IConfigurationGateway gateway, ITaggableKind kind, string dataStoreName, Guid tagId, CancellationToken cancellationToken = default)
        => Read(gateway, kind, dataStoreName, "TagId", tagId, cancellationToken);

    /// <inheritdoc />
    public override async Task<IGenericResult<IReadOnlyList<TagLinkRow>>> ForKind(
        IConfigurationGateway gateway, ITaggableKind kind, string dataStoreName, CancellationToken cancellationToken = default)
    {
        var command = new QueryCommandBuilder<TLink>(dataStoreName, kind.PathName, kind.ContainerName)
            .Where("IsCurrent", true)
            .Where("IsDeleted", false)
            .Build().Command;

        var rows = await gateway.Execute<IEnumerable<TLink>>(command, Target(kind, dataStoreName), cancellationToken).ConfigureAwait(false);
        return rows.IsSuccess
            ? GenericResult<IReadOnlyList<TagLinkRow>>.Success([.. (rows.Value ?? []).Select(Row)])
            : rows.ToNewResult<IReadOnlyList<TagLinkRow>>();
    }

    /// <inheritdoc />
    public override Task<IGenericResult<ConfigurationWriteResult>> Insert(
        IConfigurationGateway gateway, ITaggableKind kind, string dataStoreName, Guid thingId, Guid tagId, CancellationToken cancellationToken = default)
        => gateway.Execute<ConfigurationWriteResult>(
            new ConfigurationSaveCommand<TLink>(Make(thingId, tagId), replacesCurrent: false),
            Target(kind, dataStoreName),
            cancellationToken);

    /// <inheritdoc />
    public override Task<IGenericResult<ConfigurationWriteResult>> Retire(
        IConfigurationGateway gateway, ITaggableKind kind, string dataStoreName, Guid linkId, CancellationToken cancellationToken = default)
        => gateway.Execute<ConfigurationWriteResult>(
            new ConfigurationRetireCommand((KeyTypeBase)KeyTypes.ByName("Logical"), [linkId], markDeleted: true),
            Target(kind, dataStoreName),
            cancellationToken);

    private static DataStoreTarget Target(ITaggableKind kind, string dataStoreName)
        => new(dataStoreName, kind.PathName, kind.ContainerName);

    private async Task<IGenericResult<IReadOnlyList<TagLinkRow>>> Read(
        IConfigurationGateway gateway, ITaggableKind kind, string dataStoreName, string column, Guid value, CancellationToken cancellationToken)
    {
        var command = new QueryCommandBuilder<TLink>(dataStoreName, kind.PathName, kind.ContainerName)
            .Where(column, value)
            .Where("IsCurrent", true)
            .Where("IsDeleted", false)
            .Build().Command;

        var rows = await gateway.Execute<IEnumerable<TLink>>(command, Target(kind, dataStoreName), cancellationToken).ConfigureAwait(false);
        return rows.IsSuccess
            ? GenericResult<IReadOnlyList<TagLinkRow>>.Success([.. (rows.Value ?? []).Select(Row)])
            : rows.ToNewResult<IReadOnlyList<TagLinkRow>>();
    }
}
