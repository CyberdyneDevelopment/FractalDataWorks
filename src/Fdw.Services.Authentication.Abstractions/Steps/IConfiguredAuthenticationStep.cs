using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Context;
using Fdw.Services.Multitenancy.Abstractions;

namespace Fdw.Services.Authentication.Abstractions.Steps;

/// <summary>
/// A step that reads its own settings from the flow row that names it.
/// </summary>
/// <remarks>
/// <para>
/// A step is one instance for the life of the process and serves every flow that names it, so the
/// settings cannot live on the step. The runner hands the step the settings of the flow it is running,
/// on every call.
/// </para>
/// <para>
/// The settings are checked when the flow loads, through <see cref="ValidateConfiguration"/>. A flow
/// whose settings the step refuses is known-invalid and never runs, so malformed JSON is reported with
/// the flow's name at load time rather than at somebody's sign-in.
/// </para>
/// </remarks>
public interface IConfiguredAuthenticationStep : IAuthenticationStep
{
    /// <summary>Checks the settings a flow gives this step.</summary>
    /// <param name="configuration">The step row's Configuration column; null when the row carries none.</param>
    /// <returns>Success when the step can run with these settings; a failure naming what is wrong otherwise.</returns>
    IGenericResult ValidateConfiguration(string? configuration);

    /// <summary>Runs the step with the settings of the flow being run.</summary>
    /// <param name="context">What the flow has established so far.</param>
    /// <param name="configuration">
    /// The step row's Configuration column for this flow. Already accepted by
    /// <see cref="ValidateConfiguration"/> when the flow loaded.
    /// </param>
    /// <param name="tenants">This request's tenant provider; see <see cref="IAuthenticationStep.Execute"/>.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The step's outcome.</returns>
    Task<IGenericResult<StepOutcome>> Execute(
        AuthenticationContext context,
        string? configuration,
        ITenantProvider tenants,
        CancellationToken cancellationToken = default);
}
