using System;

namespace Fdw.Services.Tags;

/// <summary>One current link between a thing and a tag, as a store reads it.</summary>
/// <param name="LinkId">The link row's own Id.</param>
/// <param name="ThingId">The Id of the thing that carries the tag.</param>
/// <param name="TagId">The Id of the tag.</param>
public sealed record TagLinkRow(Guid LinkId, Guid ThingId, Guid TagId);
