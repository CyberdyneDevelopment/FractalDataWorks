using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Quality;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>What the flows over the versions of a glossary term read and ask (FDW-2331).</summary>
/// <param name="Terms">Reads the terms, which are the tenant's versions.</param>
/// <param name="Versions">Reads and writes the versions held at organizations and workspaces.</param>
/// <param name="Ancestry">Says which places are at or above a place.</param>
/// <param name="Authority">Says whether the caller holds a right at a place.</param>
internal sealed record GlossaryTermVersionServices(
    IGlossaryTermConfigurationProvider Terms,
    IGlossaryTermVersionConfigurationProvider Versions,
    IPlaceAncestry Ancestry,
    IScopeAuthority Authority);
