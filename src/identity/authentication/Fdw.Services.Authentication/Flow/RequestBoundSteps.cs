using System;
using Fdw.Services.Authentication.Abstractions.Steps;

namespace Fdw.Services.Authentication.Flow;

/// <summary>
/// Gives a step that needs request services the ones of the request that is running it (FDW-4640).
/// </summary>
/// <remarks>
/// The runner is scoped and its registration holds the request's own provider, so the lookup the runner is built with is wrapped here: a step that is request-bound is asked to
/// bind to the carrier resolved from that scope, and any other step is returned as it is. The step in the collection is never changed, so two requests in two scopes never share
/// what one of them was given.
/// </remarks>
public static class RequestBoundSteps
{
    /// <summary>Returns the step as the request sees it.</summary>
    /// <param name="step">The step the collection answered, or null when none did.</param>
    /// <param name="requestServices">Resolves a carrier type from the request's own scope.</param>
    /// <returns>The bound step for a request-bound step; <paramref name="step"/> itself otherwise.</returns>
    public static IAuthenticationStep? ForRequest(IAuthenticationStep? step, Func<Type, object> requestServices)
    {
        if (requestServices is null) throw new ArgumentNullException(nameof(requestServices));

        return step is IRequestBoundAuthenticationStep bound ? bound.BindToRequest(requestServices(bound.RequestServicesType)) : step;
    }
}
