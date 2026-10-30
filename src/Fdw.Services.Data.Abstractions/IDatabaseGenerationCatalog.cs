using System.Collections.Generic;

namespace Fdw.Services.Data.Abstractions;

/// <summary>
/// Lists the databases a host reads configuration from, as the configuration gateways it has built tell them, so something
/// that watches every one of them finds them without a list of connection names of its own to keep up to date.
/// </summary>
/// <remarks>
/// Separate from <see cref="IConfigurationGatewayProvider"/>: that interface hands back one gateway by name, and
/// adding a member to it would break every stand-in of it.
/// </remarks>
public interface IDatabaseGenerationCatalog
{
    /// <summary>Lists the sources held now: the gateways that have been asked for, which are the ones whose reads are cached.</summary>
    /// <returns>The sources, in no particular order.</returns>
    IReadOnlyList<IDatabaseGenerationSource> Sources();
}
