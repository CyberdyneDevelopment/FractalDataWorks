using Fdw.Services.Quality;
using Fdw.Services.Quality.Configuration;
using Fdw.Services.Workspaces;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>Where the versions of glossary terms are read from, stated for the shared reads of versions held at places (FDW-2331, FDW-2560).</summary>
internal static class GlossaryTermVersionSource
{
    internal static VersionSource<GlossaryTermVersionDomainConfiguration> Of(IGlossaryTermVersionConfigurationProvider versions) => new(
        versions,
        nameof(GlossaryTermVersionDomainConfiguration.BaseTermId),
        nameof(GlossaryTermVersionDomainConfiguration.ScopeId),
        row => row.BaseTermId,
        row => row.ScopeKind,
        row => row.ScopeId);
}
