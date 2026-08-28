using System.Diagnostics.CodeAnalysis;
using Fdw.Abstractions;
using Fdw.Collections;
using Fdw.Services;
using Fdw.Services.Abstractions;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.ServiceTypes;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Execution;
using Fdw.Services.Authentication.Execution;
using Fdw.Services.Authentication.Steps;
using Fdw.Services.Configuration;
using Fdw.Services.TokenManagers.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System;
using Microsoft.Extensions.Hosting;

namespace Fdw.Services.Authentication.Flow;

/// <summary>
/// Every step a flow can name.
/// </summary>
/// <remarks>
/// <para>
/// One collection, whatever a step contributes. Splitting it per contribution would buy compile-time
/// typing and cost the open set — it breaks any step contributing two things, and forces a per-stage
/// configuration schema instead of the flat ordered list that keeps a flow readable. What a step
/// needs is checked when the flow loads instead, which catches the same mistakes earlier and by
/// name.
/// </para>
/// <para>
/// A package declaring a step and being referenced is what makes it selectable — there is no
/// registry to edit and no switch to extend. Removing the reference makes every flow naming that
/// step fail at startup rather than silently doing less than it used to.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[ServiceTypeCollection(
    typeof(AuthenticationStepTypeBase<IGenericService, IServiceFactory<IGenericService, IServiceConfiguration>>),
    typeof(IAuthenticationStepType),
    typeof(AuthenticationStepTypes),
    ServiceCategory = "AuthenticationStep")]
public partial class AuthenticationStepTypes : ServiceTypeCollectionBase<
    AuthenticationStepTypeBase<IGenericService, IServiceFactory<IGenericService, IServiceConfiguration>>,
    IAuthenticationStepType>
{
    /// <summary>
    /// Registers what a flow needs beyond the step options themselves.
    /// </summary>
    /// <remarks>
    /// The resolver, the flow provider and the runner are one registration each for the whole
    /// collection, so they belong here rather than in an application's startup — a host that had to
    /// call an AddXxx of its own is a host the next one has to remember to copy.
    /// </remarks>
    static AuthenticationStepTypes()
    {
        var collectOptions = RegisterFunc;

        Registration((builder, loggerFactory) =>
        {
            collectOptions?.Invoke(builder, loggerFactory);

            builder.Services.TryAddSingleton<AuthenticationStepResolver>(sp =>
                new AuthenticationStepResolver(sp.GetService<ILogger<AuthenticationStepResolver>>()));

            builder.Services.TryAddSingleton<IAuthenticationStepResolver>(sp =>
                sp.GetRequiredService<AuthenticationStepResolver>());

            builder.Services.TryAddSingleton<IAcrPolicy, StandardAcrPolicy>();

            builder.Services.TryAddSingleton<IAuthenticationExecutionStore>(sp =>
                new InMemoryExecutionStore(sp.GetService<ILogger<InMemoryExecutionStore>>()));

            builder.Services.TryAddSingleton<IAuthorizationRequestStore>(sp =>
                new InMemoryAuthorizationRequestStore(
                    sp.GetService<ILogger<InMemoryAuthorizationRequestStore>>()));

            builder.Services.TryAddSingleton<IAuthenticationFlowProvider>(sp =>
                new AuthenticationFlowProvider(
                    sp.GetRequiredService<ImplementationConfigurationProviderBase<
                        AuthenticationFlowConfiguration, AuthenticationFlowConfigurationCommand>>(),
                    sp.GetRequiredService<ImplementationConfigurationProviderBase<
                        AuthenticationFlowStepConfiguration, AuthenticationFlowStepConfigurationCommand>>(),
                    sp.GetRequiredService<AuthenticationStepResolver>(),
                    sp.GetService<ILogger<AuthenticationFlowProvider>>()));

            builder.Services.TryAddScoped(sp => new AuthenticationRunner(
                sp.GetRequiredService<IAuthenticationStepResolver>(),
                sp.GetRequiredService<IAcrPolicy>(),
                sp.GetRequiredService<ITokenIssuer>(),
                sp.GetRequiredService<IAuthenticationExecutionStore>(),
                TimeSpan.FromMinutes(10),
                sp.GetService<ILogger<AuthenticationRunner>>()));

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
    }

    /// <summary>
    /// The connection this domain's configuration rows are read from and written to.
    /// </summary>
    /// <remarks>
    /// ServerConfiguration rather than PlatformConfiguration: which providers a host accepts, and on
    /// what terms, is that host's business. Two hosts in one tenant legitimately differ. The binding
    /// between a provider subject and a user is the opposite — a fact about the tenant — and reads
    /// from PlatformConfiguration.
    /// </remarks>
    public static string ConfigurationConnection { get; set; } = "ServerConfiguration";
}
