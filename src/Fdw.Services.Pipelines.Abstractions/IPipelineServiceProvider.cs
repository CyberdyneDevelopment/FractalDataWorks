using Fdw.Abstractions;
using Fdw.ServiceTypes;

namespace Fdw.Services.Pipelines.Abstractions;

/// <summary>
/// Resolves configured pipeline services by name or id, and builds one from a configuration.
/// </summary>
/// <remarks>
/// Named rather than a bare closed generic: a constructor asking for this states which domain it
/// resolves. Closed over the domain's implementation contract as well as the service, so the
/// registration surface — <c>Register</c>, both configuration overloads and the configuration-taking
/// <c>Get</c> — is on the interface. A provider closed over the service alone hides all of them, and a
/// domain can look wired while having no way to accept a configuration provider.
/// </remarks>
public interface IPipelineServiceProvider
    : IPlatformServiceProvider<IGenericService, IPipelineImplementationConfiguration>
{
}
