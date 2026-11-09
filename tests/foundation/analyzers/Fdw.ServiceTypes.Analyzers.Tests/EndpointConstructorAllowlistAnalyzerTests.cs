using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Fdw.ServiceTypes.Analyzers.Tests;

/// <summary>
/// FDW060 (FDW-4641 A): an endpoint takes only its domain's provider interface, a logger and the declared
/// cross-cutting types through its constructor.
/// </summary>
public class EndpointConstructorAllowlistAnalyzerTests
{
    // Appended after each snippet, never before it: a snippet opens with its usings, which must precede every
    // namespace in the file (CS1529).
    private const string Stubs = @"
namespace FastEndpoints
{
    public abstract class BaseEndpoint { }
    public abstract class Endpoint<TRequest, TResponse> : BaseEndpoint { }
    public abstract class EndpointWithoutRequest : BaseEndpoint { }
}

namespace Microsoft.Extensions.Logging
{
    public interface ILogger { }
    public interface ILogger<out TCategoryName> : ILogger { }
}

namespace Microsoft.Extensions.DependencyInjection
{
    public interface IServiceScopeFactory { }
}

namespace System.Net.Http
{
    public interface IHttpClientFactory { }
}

namespace Fdw.Services.Abstractions
{
    [System.AttributeUsage(System.AttributeTargets.Interface | System.AttributeTargets.Class)]
    public sealed class EndpointCrossCuttingAttribute : System.Attribute { }
}

namespace Fdw.Services.Data.Abstractions
{
    public interface IDataGateway { }
    public interface IConfigurationGateway { }
    public interface IConfigurationGatewayProvider { }
    public interface IDataGatewayProvider { }
}

namespace Sample
{
    public sealed class Req { }
    public sealed class Res { }
    public interface IConnectionProvider { }
    public interface IExecutionTracker { }
    public interface ITenantContext { }
    [Fdw.Services.Abstractions.EndpointCrossCutting]
    public interface IAdministrativeAudit { }
    public interface IUserConfigurationProvider { }
    public class UserConfigurationProvider : IUserConfigurationProvider { }
}
";

    private const string EnableRule = "dotnet_diagnostic.FDW060.severity = error";
    private const string AllowTenantContext = "fdw_endpoint_cross_cutting_types = Sample.ITenantContext, Sample.Unused";

    private static Task Run(string source, string editorConfig, params DiagnosticResult[] expected)
    {
        var test = new CSharpAnalyzerTest<EndpointConstructorAllowlistAnalyzer, DefaultVerifier>
        {
            TestCode = source + Stubs,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };
        test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", "root = true\n\n[*]\n" + editorConfig + "\n"));
        test.ExpectedDiagnostics.AddRange(expected);
        return test.RunAsync(TestContext.Current.CancellationToken);
    }

    private static DiagnosticResult Expected(int location, string endpoint, string type, string parameter, string reason)
        => new DiagnosticResult(EndpointConstructorAllowlistAnalyzer.DiagnosticId, DiagnosticSeverity.Error)
            .WithLocation(location)
            .WithArguments(endpoint, type, parameter, reason);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Analyzer")]
    public Task ProviderLoggerAndCrossCuttingTypesAreSilent() => Run(@"
using Microsoft.Extensions.Logging;
using Sample;

public sealed class TestConnectionEndpoint : FastEndpoints.Endpoint<Req, Res>
{
    public TestConnectionEndpoint(
        IConnectionProvider connections,
        IUserConfigurationProvider users,
        ITenantContext tenant,
        IAdministrativeAudit audit,
        ILogger<TestConnectionEndpoint> logger,
        ILogger plainLogger)
    {
    }
}
", EnableRule + "\n" + AllowTenantContext);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Analyzer")]
    public Task EachRefusedShapeReportsWithItsReason() => Run(@"
using System;
using System.Net.Http;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Sample;

public sealed class ListThingsEndpoint : FastEndpoints.EndpointWithoutRequest
{
    public ListThingsEndpoint(
        IDataGateway {|#0:gateway|},
        IConfigurationGateway {|#1:configurationGateway|},
        IConfigurationGatewayProvider {|#2:gatewayProvider|},
        IDataGatewayProvider {|#3:dataGatewayProvider|},
        IServiceProvider {|#4:services|},
        IServiceScopeFactory {|#5:scopes|},
        IHttpClientFactory {|#6:clients|},
        IExecutionTracker {|#7:tracker|},
        UserConfigurationProvider {|#8:users|},
        TimeProvider {|#9:time|})
    {
    }
}
", EnableRule,
        Expected(0, "ListThingsEndpoint", "IDataGateway", "gateway", "a gateway (an endpoint holds no gateway)"),
        Expected(1, "ListThingsEndpoint", "IConfigurationGateway", "configurationGateway", "a gateway (an endpoint holds no gateway)"),
        Expected(2, "ListThingsEndpoint", "IConfigurationGatewayProvider", "gatewayProvider", "a gateway (an endpoint holds no gateway)"),
        Expected(3, "ListThingsEndpoint", "IDataGatewayProvider", "dataGatewayProvider", "a gateway (an endpoint holds no gateway)"),
        Expected(4, "ListThingsEndpoint", "IServiceProvider", "services", "a service locator"),
        Expected(5, "ListThingsEndpoint", "IServiceScopeFactory", "scopes", "a service locator"),
        Expected(6, "ListThingsEndpoint", "IHttpClientFactory", "clients", "an ad hoc client factory"),
        Expected(7, "ListThingsEndpoint", "IExecutionTracker", "tracker", "a service interface, not a provider interface"),
        Expected(8, "ListThingsEndpoint", "UserConfigurationProvider", "users", "a concrete type, not an interface"),
        Expected(9, "ListThingsEndpoint", "TimeProvider", "time", "a concrete type, not an interface"));

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Analyzer")]
    public Task AnEndpointBaseAndItsForwarderBothReport() => Run(@"
using Sample;

public abstract class ListThingsEndpointBase : FastEndpoints.Endpoint<Req, Res>
{
    protected ListThingsEndpointBase(IExecutionTracker {|#0:tracker|})
    {
    }
}

public sealed class ListThingsEndpoint : ListThingsEndpointBase
{
    public ListThingsEndpoint(IExecutionTracker {|#1:tracker|})
        : base(tracker)
    {
    }
}
", EnableRule,
        Expected(0, "ListThingsEndpointBase", "IExecutionTracker", "tracker", "a service interface, not a provider interface"),
        Expected(1, "ListThingsEndpoint", "IExecutionTracker", "tracker", "a service interface, not a provider interface"));

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Analyzer")]
    public Task WithoutTheCrossCuttingKeyACrossCuttingInterfaceIsRefused() => Run(@"
using Sample;

public sealed class WhoAmIEndpoint : FastEndpoints.EndpointWithoutRequest
{
    public WhoAmIEndpoint(ITenantContext {|#0:tenant|})
    {
    }
}
", EnableRule,
        Expected(0, "WhoAmIEndpoint", "ITenantContext", "tenant", "a service interface, not a provider interface"));

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Analyzer")]
    public Task AClassThatIsNotAnEndpointIsSilent() => Run(@"
using Fdw.Services.Data.Abstractions;
using Sample;

public sealed class ThingService
{
    public ThingService(IDataGateway gateway, IExecutionTracker tracker)
    {
    }
}
", EnableRule);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Analyzer")]
    public void TheRuleIsOffUntilEditorConfigTurnsItOn()
    {
        // What a rule ships as is its descriptors' IsEnabledByDefault. The test harness enables every descriptor of the analyzer it
        // runs, so running the analyzer with an empty editorconfig cannot show that the rule is off (FDW-4716).
        var supported = new EndpointConstructorAllowlistAnalyzer().SupportedDiagnostics;

        Assert.NotEmpty(supported);
        Assert.All(supported, descriptor => Assert.False(descriptor.IsEnabledByDefault, descriptor.Id));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Analyzer")]
    public Task AFolderLineSetToNoneSilencesIt() => Run(@"
using Fdw.Services.Data.Abstractions;

public sealed class ListThingsEndpoint : FastEndpoints.EndpointWithoutRequest
{
    public ListThingsEndpoint(IDataGateway gateway)
    {
    }
}
", EnableRule + "\n\n[/0/**.cs]\ndotnet_diagnostic.FDW060.severity = none");
}
