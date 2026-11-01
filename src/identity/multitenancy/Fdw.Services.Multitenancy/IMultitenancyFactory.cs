using Fdw.Abstractions;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Multitenancy;

/// <summary>
/// Marker factory type for the multitenancy domain. The domain has no runtime-resolved service
/// instance — each option's <c>Register</c> phase registers tenant/org infrastructure
/// directly — so no option ever constructs an <see cref="IGenericService"/> through this factory.
/// It exists solely to satisfy <see cref="Fdw.ServiceTypes.ServiceTypeBase{TService,TFactory,TConfiguration}"/>'s
/// generic constraints, the same role <c>IAuthorizationFactory</c> plays for the Authorization domain.
/// </summary>
public interface IMultitenancyFactory : IServiceFactory<IGenericService, IServiceConfiguration>
{
}
