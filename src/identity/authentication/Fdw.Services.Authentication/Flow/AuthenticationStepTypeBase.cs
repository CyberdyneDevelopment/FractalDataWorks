using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Messages;
using Fdw.Services.Abstractions;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authentication.Logging;
using Fdw.ServiceTypes;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authentication.Flow;

/// <summary>
/// Base for step options.
/// </summary>
/// <typeparam name="TService">The step this option produces.</typeparam>
/// <typeparam name="TFactory">The factory that produces it.</typeparam>
/// <remarks>
/// One collection holds every step, whatever it contributes. Splitting it per contribution would buy
/// compile-time typing and cost the open set — it breaks any step contributing two things, and forces
/// a per-stage configuration schema instead of a flat ordered list. What a step needs is checked when
/// the flow loads instead.
/// </remarks>
public abstract class AuthenticationStepTypeBase<TService, TFactory> :
    ServiceTypeBase<TService, TFactory, IServiceConfiguration>,
    IAuthenticationStepType
    where TService : IGenericService
    where TFactory : IServiceFactory<TService, IServiceConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="AuthenticationStepTypeBase{TService, TFactory}"/> class.</summary>
    /// <param name="name">The name a flow uses to select this step.</param>
    /// <param name="sectionName">The configuration section this option binds from.</param>
    /// <param name="displayName">The display name.</param>
    /// <param name="description">The description.</param>
    protected AuthenticationStepTypeBase(
        string name, string sectionName, string displayName, string description)
        : base(name, sectionName, displayName, description, category: "AuthenticationStep")
    {
    }

    /// <summary>Reports that this step was asked to run before the host gave it what it needs.</summary>
    /// <param name="logger">The logger to report through.</param>
    /// <returns>The logged message, for the failed result.</returns>
    /// <remarks>
    /// Here rather than in each step's own log so that every step reports the same failure with the same
    /// code, and so a step in another package does not need access to an internal log.
    /// </remarks>
    protected IGenericMessage NotInitialized(ILogger logger) => StepLog.NotInitialized(logger, Name);
}
