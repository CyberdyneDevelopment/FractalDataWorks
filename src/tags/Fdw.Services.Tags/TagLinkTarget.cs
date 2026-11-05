using Fdw.Services.Data.Abstractions;
using Fdw.Services.Tags.Abstractions;

namespace Fdw.Services.Tags;

/// <summary>What a link operation works on: the kind, its store and the gateway onto the configuration store.</summary>
/// <param name="Kind">The taggable kind.</param>
/// <param name="Store">The kind's link store.</param>
/// <param name="Gateway">The gateway onto the configuration store.</param>
internal sealed record TagLinkTarget(ITaggableKind Kind, TagLinkStoreBase Store, IConfigurationGateway Gateway);
