using Fdw.Abstractions;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Authentication.Validation;

/// <summary>
/// Marker factory type for the inbound-token validation domain.
/// </summary>
/// <remarks>
/// The domain has no runtime-resolved service instance — each option's <c>Register</c> phase adds an
/// ASP.NET authentication scheme directly, and the scheme is what does the work from then on. This
/// interface exists solely to satisfy
/// <see cref="Fdw.ServiceTypes.ServiceTypeBase{TService,TFactory,TConfiguration}"/>'s generic
/// constraints, the same role <c>IMultitenancyFactory</c> plays for the multitenancy domain.
/// </remarks>
public interface IAuthenticationValidationFactory : IServiceFactory<IGenericService, IServiceConfiguration>
{
}
