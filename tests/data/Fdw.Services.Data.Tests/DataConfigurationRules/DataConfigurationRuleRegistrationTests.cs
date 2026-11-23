using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Abstractions.DataConfigurationRules;
using Fdw.Services.Data.DataConfigurationRules;
using Fdw.Services.Descriptions;
using Fdw.Services.Descriptions.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Settings;
using Fdw.Services.Workspaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Tests.DataConfigurationRules;

/// <summary>
/// FDW-6346 (Release-Supervisor's condition on FDW-6489): the rule service and the names directory take services other domains register, and a host that lacks one does not start
/// (PlatformServices.Configure turns on ValidateOnBuild in every environment). The data gateway's own Register reads the configuration store, so no unit test can run it; this test
/// runs what can run without a database, the registrations of the workspaces, authorization and descriptions domains (the rule service writes and reads each rule's description with the
/// descriptions writer and reader, FDW-6208), adds the two registrations the data gateway makes, and walks every constructor
/// dependency of the two services through the registrations the way ValidateOnBuild reads them. Everything that is still unregistered is named in <see cref="HostContract"/>: what the
/// other domains of a host (settings, tenancy, operations, authentication and the data set provider) must register. A new dependency that no domain registers, as IWorkspaceAccessPolicy
/// was not, shows here.
/// </summary>
/// <remarks>
/// What this does not prove: a registration made by a factory (<c>sp =&gt; new X(...)</c>) is not read, so what such a factory resolves is not walked; and the host contract itself is
/// only a list. The started hosts (build-agent's pre-release proof) are the real check.
/// </remarks>
public sealed class DataConfigurationRuleRegistrationTests
{
    // What a host registers through the domains this test does not run; each is a stand-in here.
    private static readonly Type[] HostContract =
    [
        typeof(IConfigurationGatewayProvider),
        typeof(IEffectiveSettingsProvider),
        typeof(ITenantContext),
        typeof(IAdministrativeAudit),
        typeof(IAuthenticationContextAccessor),
        typeof(IUserNames),
        typeof(IOrganizationProvider),
        typeof(IDataSetConfigurationProvider),
        typeof(IDataStoreProvider),
    ];

    [Fact]
    public void EveryConstructorDependencyOfTheRuleServicesIsRegisteredByAnOwningDomainOrNamedInTheHostContract()
    {
        var services = Composition(withWorkspaces: true, withAuthorization: true);

        Unregistered(services, typeof(IDataConfigurationRules), typeof(IDataConfigurationRuleNames)).ShouldBeEmpty();
    }

    [Fact]
    public void AMissingWorkspacesDomainIsReportedByNamingWhatTheRuleServicesTake()
    {
        var services = Composition(withWorkspaces: false, withAuthorization: true);

        var missing = Unregistered(services, typeof(IDataConfigurationRules), typeof(IDataConfigurationRuleNames));

        missing.ShouldContain(typeof(IWorkspaceAccessPolicy).FullName!);
        missing.ShouldContain(typeof(IWorkspaceConfigurationProvider).FullName!);
    }

    [Fact]
    public void AMissingAuthorizationDomainIsReportedByNamingTheHolders()
    {
        var services = Composition(withWorkspaces: true, withAuthorization: false);

        Unregistered(services, typeof(IDataConfigurationRules), typeof(IDataConfigurationRuleNames)).ShouldContain("Fdw.Services.Authorization.Abstractions.IPermissionHolders");
    }

    [Fact]
    public void AMissingDescriptionsDomainIsReportedByNamingTheWriterAndTheReader()
    {
        var services = Composition(withWorkspaces: true, withAuthorization: true, withDescriptions: false);

        var missing = Unregistered(services, typeof(IDataConfigurationRules), typeof(IDataConfigurationRuleNames));

        missing.ShouldContain(typeof(IDescriptionWriter).FullName!);
        missing.ShouldContain(typeof(IDescriptionReader).FullName!);
    }

    // A stand-in for a host contract type known only at run time.
    private static object StandIn(Type contract) => ((Mock)Activator.CreateInstance(typeof(Mock<>).MakeGenericType(contract))!).Object;

    private static IServiceCollection Composition(bool withWorkspaces, bool withAuthorization, bool withDescriptions = true)
    {
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
        builder.Services.AddLogging();
        foreach (var contract in HostContract)
            builder.Services.AddSingleton(contract, StandIn(contract));

        if (withWorkspaces)
            new DefaultWorkspaceServiceType().Register(builder, NullLoggerFactory.Instance).IsSuccess.ShouldBeTrue();
        if (withAuthorization)
            new DefaultAuthorizationServiceType().Register(builder, NullLoggerFactory.Instance).IsSuccess.ShouldBeTrue();
        if (withDescriptions)
            new DefaultDescriptionServiceType().Register(builder, NullLoggerFactory.Instance).IsSuccess.ShouldBeTrue();

        // The two registrations the data gateway makes (MainDataGatewayImplementation): its Register reads the configuration store first, so a test cannot run it.
        builder.Services.TryAddScoped<IDataConfigurationRules, DataConfigurationRuleService>();
        builder.Services.TryAddScoped<IDataConfigurationRuleNames, DataConfigurationRuleNames>();
        return builder.Services;
    }

    // The names of the service types a constructor chain needs and nothing registers, starting from the roots. A registration by implementation type is read as ValidateOnBuild reads it
    // (the constructor with the most parameters; a parameter with a default is optional); a registration by factory is taken as made.
    private static List<string> Unregistered(IServiceCollection services, params Type[] roots)
    {
        var missing = new SortedSet<string>(StringComparer.Ordinal);
        var seen = new HashSet<Type>();
        var queue = new Queue<Type>(roots);
        while (queue.Count > 0)
        {
            var type = queue.Dequeue();
            if (!seen.Add(type))
                continue;

            // The framework's own services (logging, options) are not the domains' to register, and an IEnumerable of a service is satisfied by any number of registrations.
            if (type.ContainsGenericParameters || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
                continue;
            if (type.Namespace is { } space && space.StartsWith("Microsoft.Extensions", StringComparison.Ordinal))
                continue;

            var found = services.Where(d => !d.IsKeyedService && (d.ServiceType == type || (type.IsGenericType && d.ServiceType == type.GetGenericTypeDefinition()))).ToList();
            if (found.Count == 0)
            {
                missing.Add(type.FullName ?? type.Name);
                continue;
            }

            foreach (var implementation in found.Select(d => d.ImplementationType).OfType<Type>())
            {
                var constructor = implementation.GetConstructors().OrderByDescending(c => c.GetParameters().Length).First();
                foreach (var parameter in constructor.GetParameters().Where(p => !p.HasDefaultValue))
                    queue.Enqueue(parameter.ParameterType);
            }
        }

        return [.. missing];
    }
}
