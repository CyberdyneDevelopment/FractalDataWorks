using System;

namespace Fdw.Services.Authentication.Abstractions.Steps;

/// <summary>
/// A step that needs a service which really lives for one request, and is handed it by the runner for the execution instead of capturing it at Initialize (FDW-4640).
/// </summary>
/// <remarks>
/// <para>
/// Why this exists: a step is one instance for the process (see <see cref="IAuthenticationStep"/>), built before any request, and Initialize runs against the root provider, which
/// refuses a scoped service. A service that is stateless and made only from singletons is registered as a singleton and captured at Initialize like any other. A service whose own
/// dependencies are scoped (the credential service, a domain service provider) cannot be, and a step that kept one scope's instance would serve every later login with it.
/// </para>
/// <para>
/// How it works: the step names the type that carries what it needs per request, and the runner's registration, which is scoped, resolves that type from the request's own scope
/// and asks the step to <see cref="BindToRequest"/>. What comes back is the step as that request sees it, and is the step the runner executes. The carrier is a type of the step's
/// own package, registered scoped there, that holds the scoped services behind <c>Func</c> hand-offs; a step never takes a service provider or a scope factory.
/// </para>
/// </remarks>
public interface IRequestBoundAuthenticationStep
{
    /// <summary>The type that carries the services this step needs for one request; registered scoped by the step's own registration.</summary>
    Type RequestServicesType { get; }

    /// <summary>Returns the step as one request sees it, holding that request's services.</summary>
    /// <param name="services">The instance of <see cref="RequestServicesType"/> resolved from the request's own scope.</param>
    /// <returns>A step that holds <paramref name="services"/> and shares everything else with this one; this step is not changed.</returns>
    IAuthenticationStep BindToRequest(object services);
}
