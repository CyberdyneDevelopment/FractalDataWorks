using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Tags.Abstractions;
using Fdw.Services.Tags.Configuration;
using Fdw.Services.Tags.Logging;
using Fdw.Services.Tags.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Tags;

/// <summary>
/// The Tags service. A tag is a domain row (<c>tag.Tag</c>) made through the tag provider; a link between a thing and a tag is one row of the
/// kind's own link table, inserted or retired through the kind's link store. Nothing here is defaulted: a missing input is a failed result
/// that names it.
/// </summary>
public sealed class TagService : ITagService
{
    private const int NameMaxLength = 100;
    private const string DomainName = "Tag";

    private readonly ITagConfigurationProvider _tags;
    private readonly IConfigurationGatewayProvider _gateways;
    private readonly ITagScope _scope;
    private readonly string _dataStoreName;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="TagService"/> class.</summary>
    /// <param name="tags">Reads and writes the tags.</param>
    /// <param name="gateways">Supplies the gateway onto the configuration store, for the link tables.</param>
    /// <param name="scope">Decides whether a tag may be put on a thing.</param>
    /// <param name="dataStoreName">The configuration connection the tag and link tables live on.</param>
    /// <param name="logger">The logger.</param>
    public TagService(
        ITagConfigurationProvider tags,
        IConfigurationGatewayProvider gateways,
        ITagScope scope,
        string dataStoreName,
        ILogger<TagService>? logger = null)
    {
        _tags = tags;
        _gateways = gateways;
        _scope = scope;
        _dataStoreName = dataStoreName;
        _logger = logger ?? NullLogger<TagService>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<TagDomainConfiguration>> Create(
        string name, string ownerKind, Guid ownerId, string? description, CancellationToken cancellationToken = default)
    {
        var valid = ValidName(name);
        if (valid.IsFailure) return valid.ToNewResult<TagDomainConfiguration>();

        var held = await Find(name, cancellationToken).ConfigureAwait(false);
        if (held.IsSuccess && held.Value is { } existing)
            return Refuse<TagDomainConfiguration>("TagNameTaken", "name", name, "ownerKind", existing.OwnerKind, "ownerId", existing.OwnerId.ToString("D"));

        var saved = await _tags.Save<ITagImplementationConfiguration>(
            new TagImplementationConfiguration { Name = name, Description = description, OwnerKind = ownerKind, OwnerId = ownerId },
            DomainName, DomainName, name, cancellationToken).ConfigureAwait(false);
        return saved.IsSuccess ? await Find(name, cancellationToken).ConfigureAwait(false) : saved.ToNewResult<TagDomainConfiguration>();
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Delete(string name, CancellationToken cancellationToken = default)
    {
        var tag = await Find(name, cancellationToken).ConfigureAwait(false);
        if (tag.IsFailure || tag.Value is not { } found) return tag;

        var retired = await RetireLinks(found, cancellationToken).ConfigureAwait(false);
        return retired.IsFailure ? retired : await _tags.Delete(found.Id, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Link(string kind, Guid thingId, string tagName, CancellationToken cancellationToken = default)
    {
        var target = Resolve(kind);
        if (target.IsFailure || target.Value is not { } resolved) return target;

        var tag = await Find(tagName, cancellationToken).ConfigureAwait(false);
        if (tag.IsFailure || tag.Value is not { } found) return tag;

        var allowed = await _scope.Allows(found, resolved.Kind, thingId, cancellationToken).ConfigureAwait(false);
        if (allowed.IsFailure) return allowed;

        var links = await resolved.Store.ForThing(resolved.Gateway, resolved.Kind, _dataStoreName, thingId, cancellationToken).ConfigureAwait(false);
        if (links.IsFailure || links.Value is not { } current) return links;
        if (current.Any(link => link.TagId == found.Id)) return Refuse<bool>("TagLinkExists", "kind", kind, "thingId", thingId.ToString("D"), "name", tagName);

        TagsLog.Linking(_logger, tagName, kind, thingId);
        var inserted = await resolved.Store.Insert(resolved.Gateway, resolved.Kind, _dataStoreName, thingId, found.Id, cancellationToken).ConfigureAwait(false);
        if (inserted.IsSuccess) TagsLog.Linked(_logger, tagName, kind, thingId);
        return inserted;
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Unlink(string kind, Guid thingId, string tagName, CancellationToken cancellationToken = default)
    {
        var target = Resolve(kind);
        if (target.IsFailure || target.Value is not { } resolved) return target;

        var tag = await Find(tagName, cancellationToken).ConfigureAwait(false);
        if (tag.IsFailure || tag.Value is not { } found) return tag;

        var links = await resolved.Store.ForThing(resolved.Gateway, resolved.Kind, _dataStoreName, thingId, cancellationToken).ConfigureAwait(false);
        if (links.IsFailure || links.Value is not { } current) return links;

        var held = current.FirstOrDefault(link => link.TagId == found.Id);
        if (held is null) return Refuse<bool>("TagLinkAbsent", "kind", kind, "thingId", thingId.ToString("D"), "name", tagName);

        var retired = await resolved.Store.Retire(resolved.Gateway, resolved.Kind, _dataStoreName, held.LinkId, cancellationToken).ConfigureAwait(false);
        if (retired.IsSuccess) TagsLog.Unlinked(_logger, tagName, kind, thingId);
        return retired;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<string>>> TagsOf(string kind, Guid thingId, CancellationToken cancellationToken = default)
    {
        var target = Resolve(kind);
        if (target.IsFailure || target.Value is not { } resolved) return target.ToNewResult<IReadOnlyList<string>>();

        var links = await resolved.Store.ForThing(resolved.Gateway, resolved.Kind, _dataStoreName, thingId, cancellationToken).ConfigureAwait(false);
        if (links.IsFailure || links.Value is not { } current) return links.ToNewResult<IReadOnlyList<string>>();
        if (current.Count == 0) return GenericResult<IReadOnlyList<string>>.Success([]);

        var tags = await _tags.Get(current.Select(link => link.TagId).ToArray(), cancellationToken).ConfigureAwait(false);
        return tags.IsSuccess && tags.Value is { } rows
            ? GenericResult<IReadOnlyList<string>>.Success([.. rows.Select(tag => tag.Name).Order(StringComparer.OrdinalIgnoreCase)])
            : tags.ToNewResult<IReadOnlyList<string>>();
    }

    /// <inheritdoc />
    public Task<IGenericResult<TagDomainConfiguration>> Get(string name, CancellationToken cancellationToken = default)
        => Find(name, cancellationToken);

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyDictionary<Guid, IReadOnlyList<string>>>> TagsByThing(string kind, CancellationToken cancellationToken = default)
    {
        var target = Resolve(kind);
        if (target.IsFailure || target.Value is not { } resolved) return target.ToNewResult<IReadOnlyDictionary<Guid, IReadOnlyList<string>>>();

        var links = await resolved.Store.ForKind(resolved.Gateway, resolved.Kind, _dataStoreName, cancellationToken).ConfigureAwait(false);
        if (links.IsFailure || links.Value is not { } current) return links.ToNewResult<IReadOnlyDictionary<Guid, IReadOnlyList<string>>>();
        if (current.Count == 0) return GenericResult<IReadOnlyDictionary<Guid, IReadOnlyList<string>>>.Success(new Dictionary<Guid, IReadOnlyList<string>>());

        var tags = await _tags.Get(current.Select(link => link.TagId).Distinct().ToArray(), cancellationToken).ConfigureAwait(false);
        if (tags.IsFailure || tags.Value is not { } rows) return tags.ToNewResult<IReadOnlyDictionary<Guid, IReadOnlyList<string>>>();

        var names = rows.ToDictionary(tag => tag.Id, tag => tag.Name);
        IReadOnlyDictionary<Guid, IReadOnlyList<string>> byThing = current
            .Where(link => names.ContainsKey(link.TagId))
            .GroupBy(link => link.ThingId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<string>)[.. group.Select(link => names[link.TagId]).Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase)]);
        return GenericResult<IReadOnlyDictionary<Guid, IReadOnlyList<string>>>.Success(byThing);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<Guid>>> ThingsOf(string kind, IReadOnlyList<string> tagNames, CancellationToken cancellationToken = default)
    {
        var target = Resolve(kind);
        if (target.IsFailure || target.Value is not { } resolved) return target.ToNewResult<IReadOnlyList<Guid>>();

        IEnumerable<Guid>? carrying = null;
        foreach (var tagName in tagNames)
        {
            var tag = await Find(tagName, cancellationToken).ConfigureAwait(false);
            if (tag.IsFailure || tag.Value is not { } found) return tag.ToNewResult<IReadOnlyList<Guid>>();

            var links = await resolved.Store.ForTag(resolved.Gateway, resolved.Kind, _dataStoreName, found.Id, cancellationToken).ConfigureAwait(false);
            if (links.IsFailure || links.Value is not { } current) return links.ToNewResult<IReadOnlyList<Guid>>();

            var things = current.Select(link => link.ThingId);
            carrying = carrying is null ? things : carrying.Intersect(things);
        }

        return GenericResult<IReadOnlyList<Guid>>.Success([.. (carrying ?? []).Distinct()]);
    }

    // The tag with this name, as the list shows it: the name is read through the domain provider, then the domain row by its Id.
    private async Task<IGenericResult<TagDomainConfiguration>> Find(string name, CancellationToken cancellationToken)
    {
        var implementation = await _tags.Get(name, cancellationToken).ConfigureAwait(false);
        if (implementation.IsFailure || implementation.Value is not { } named) return Refuse<TagDomainConfiguration>("TagNotFound", "name", name);

        var rows = await _tags.Get(new[] { named.Id }, cancellationToken).ConfigureAwait(false);
        return rows.IsSuccess && rows.Value is { Count: 1 } one
            ? GenericResult<TagDomainConfiguration>.Success(one[0])
            : Refuse<TagDomainConfiguration>("TagNotFound", "name", name);
    }

    // Retires every current link of the tag in every taggable kind. A failure stops here, and running the delete again continues.
    private async Task<IGenericResult> RetireLinks(TagDomainConfiguration tag, CancellationToken cancellationToken)
    {
        foreach (var kind in TaggableKinds.All())
        {
            var target = Resolve(kind.Name);
            if (target.IsFailure || target.Value is not { } resolved) return target;

            TagsLog.RetiringLinks(_logger, tag.Name, kind.Name);
            var links = await resolved.Store.ForTag(resolved.Gateway, kind, _dataStoreName, tag.Id, cancellationToken).ConfigureAwait(false);
            if (links.IsFailure || links.Value is not { } current) return links;

            foreach (var link in current)
            {
                var retired = await resolved.Store.Retire(resolved.Gateway, kind, _dataStoreName, link.LinkId, cancellationToken).ConfigureAwait(false);
                if (retired.IsFailure) return retired;
            }
        }

        return GenericResult.Success();
    }

    private IGenericResult<TagLinkTarget> Resolve(string kindName)
    {
        var kind = TaggableKinds.ByName(kindName);
        var store = TagLinkStores.ByName(kindName);
        if (ReferenceEquals(kind, TaggableKinds.NotFound) || ReferenceEquals(store, TagLinkStores.NotFound))
        {
            return Refuse<TagLinkTarget>("TaggableKindUnknown", "kind", kindName);
        }

        var gateway = _gateways.Get(_dataStoreName);
        return gateway.IsSuccess && gateway.Value is { } connection
            ? GenericResult<TagLinkTarget>.Success(new TagLinkTarget(kind, (TagLinkStoreBase)store, connection))
            : gateway.ToNewResult<TagLinkTarget>();
    }

    private IGenericResult<bool> ValidName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return Refuse<bool>("TagNameInvalid", "name", name ?? string.Empty, "reason", "it is blank");
        return name.Length > NameMaxLength
            ? Refuse<bool>("TagNameInvalid", "name", name, "reason", $"it is longer than {NameMaxLength} characters")
            : GenericResult<bool>.Success(true);
    }

    private IGenericResult<T> Refuse<T>(string code, params object[] details)
        => GenericResult<T>.Failure(TagsResultCodes.ByName(code), _logger, TagDetails.Of(details));
}
