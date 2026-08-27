using Fdw.Results;

namespace Fdw.Services.Authentication.Abstractions.Steps;

/// <summary>
/// Finds the step a flow named.
/// </summary>
/// <remarks>
/// Synchronous: a step is a registered option, not a configuration row to be read, so resolving one
/// is a lookup rather than an I/O operation.
/// </remarks>
public interface IAuthenticationStepResolver
{
    /// <summary>Returns the step registered under <paramref name="stepName"/>.</summary>
    /// <param name="stepName">The name the flow gave.</param>
    /// <remarks>
    /// Fails when nothing is registered under that name — which is what makes removing a package a
    /// loud failure at the flows naming its step, rather than a silent degradation.
    /// </remarks>
    IGenericResult<IAuthenticationStep> Resolve(string stepName);
}
