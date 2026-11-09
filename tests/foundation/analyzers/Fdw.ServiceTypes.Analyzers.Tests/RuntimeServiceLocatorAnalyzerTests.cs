using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Fdw.ServiceTypes.Analyzers.Tests;

/// <summary>
/// FDW061 (FDW-4641 B): no runtime service locator outside a hosted worker, a registration body, an sp lambda or a
/// member marked [ServiceLocatorAllowed("reason")].
/// </summary>
public class RuntimeServiceLocatorAnalyzerTests
{
    // Appended after each snippet, never before it: a snippet opens with its usings, which must precede every
    // namespace in the file (CS1529).
    private const string Stubs = @"
namespace Microsoft.Extensions.DependencyInjection
{
    public interface IServiceCollection { }
    [Fdw.Services.Abstractions.ServiceLocatorAllowed(""framework stub"")] public interface IServiceScope : System.IDisposable { System.IServiceProvider ServiceProvider { get; } }
    public interface IServiceScopeFactory { IServiceScope CreateScope(); }
    [Fdw.Services.Abstractions.ServiceLocatorAllowed(""framework stub"")] public static class ServiceProviderServiceExtensions
    {
        public static T GetRequiredService<T>(this System.IServiceProvider provider) where T : notnull => default!;
        public static T? GetService<T>(this System.IServiceProvider provider) => default;
        public static IServiceScope CreateScope(this System.IServiceProvider provider) => default!;
    }
    public static class ServiceCollectionServiceExtensions
    {
        public static IServiceCollection AddSingleton<T>(this IServiceCollection services, System.Func<System.IServiceProvider, T> factory) where T : class => services;
    }
}

namespace Microsoft.Extensions.Hosting
{
    public interface IHostApplicationBuilder { Microsoft.Extensions.DependencyInjection.IServiceCollection Services { get; } }
    [Fdw.Services.Abstractions.ServiceLocatorAllowed(""framework stub"")] public interface IHost { System.IServiceProvider Services { get; } }
    public interface IHostedService { }
    public abstract class BackgroundService : IHostedService { }
}

namespace Microsoft.AspNetCore.Http
{
    [Fdw.Services.Abstractions.ServiceLocatorAllowed(""framework stub"")] public abstract class HttpContext { public abstract System.IServiceProvider RequestServices { get; } }
}

namespace Fdw.Services.Abstractions
{
    [System.AttributeUsage(System.AttributeTargets.All)]
    public sealed class ServiceLocatorAllowedAttribute : System.Attribute
    {
        public ServiceLocatorAllowedAttribute(string reason) { Reason = reason; }
        public string Reason { get; }
    }
}

namespace Sample
{
    public interface IThingProvider { }
    public sealed class Thing { public Thing(IThingProvider provider) { } }
}
";

    private const string EnableRule = "dotnet_diagnostic.FDW061.severity = error";

    private static Task Run(string source, string editorConfig, params DiagnosticResult[] expected)
    {
        var test = new CSharpAnalyzerTest<RuntimeServiceLocatorAnalyzer, DefaultVerifier>
        {
            TestCode = source + Stubs,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };
        test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", "root = true\n\n[*]\n" + editorConfig + "\n"));
        test.ExpectedDiagnostics.AddRange(expected);
        return test.RunAsync(TestContext.Current.CancellationToken);
    }

    private static DiagnosticResult At(int location)
        => new DiagnosticResult(RuntimeServiceLocatorAnalyzer.DiagnosticId, DiagnosticSeverity.Error).WithLocation(location);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Analyzer")]
    public Task EachLocatorShapeReports() => Run(@"
using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Sample;

public sealed class ThingStore
{
    private readonly IServiceProvider {|#0:_services|};
    private readonly IServiceScopeFactory {|#1:_scopes|};

    public ThingStore(IServiceProvider {|#2:services|}, IServiceScopeFactory {|#3:scopes|})
    {
        _services = services;
        _scopes = scopes;
    }

    public IServiceProvider {|#4:Services|} => _services;

    public void Run(HttpContext http)
    {
        var provider = {|#5:_services.GetRequiredService<IThingProvider>()|};
        using var scope = {|#6:_scopes.CreateScope()|};
        var other = {|#7:http.RequestServices|};
        var maybe = {|#8:other.GetService(typeof(IThingProvider))|};
    }
}
", EnableRule,
        At(0).WithArguments("ThingStore", "field '_services' of type IServiceProvider"),
        At(1).WithArguments("ThingStore", "field '_scopes' of type IServiceScopeFactory"),
        At(2).WithArguments("ThingStore", "parameter 'services' of type IServiceProvider"),
        At(3).WithArguments("ThingStore", "parameter 'scopes' of type IServiceScopeFactory"),
        At(4).WithArguments("ThingStore", "property 'Services' of type IServiceProvider"),
        At(5),
        At(6),
        At(7),
        At(8));

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Analyzer")]
    public Task HostedWorkersAreSilent() => Run(@"
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sample;

public sealed class ThingWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;

    public ThingWorker(IServiceScopeFactory scopes) { _scopes = scopes; }

    public void RunOnce()
    {
        using var scope = _scopes.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<IThingProvider>();
    }
}

public sealed class ThingHostedService : IHostedService
{
    public ThingHostedService(IServiceProvider services) { var p = services.GetRequiredService<IThingProvider>(); }
}
", EnableRule);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Analyzer")]
    public Task RegistrationBodiesAndSpLambdasAreSilent() => Run(@"
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sample;

public sealed class ThingType
{
    private Func<IHostApplicationBuilder, IHostApplicationBuilder>? _registration;
    private Func<IHost, IHost>? _initialization;

    public ThingType()
    {
        _registration = builder =>
        {
            builder.Services.AddSingleton(sp => new Thing(sp.GetRequiredService<IThingProvider>()));
            return builder;
        };
        _initialization = host =>
        {
            var provider = host.Services.GetRequiredService<IThingProvider>();
            return host;
        };
    }

    public static void Register(IServiceCollection services, IServiceProvider snapshot)
    {
        var provider = snapshot.GetRequiredService<IThingProvider>();
    }
}

public static class ThingWiring
{
    public static IServiceCollection Wire(IServiceCollection services)
        => services.AddSingleton(sp => new Thing(sp.GetRequiredService<IThingProvider>()));
}
", EnableRule);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Analyzer")]
    public Task AMarkWithAReasonAllowsIt() => Run(@"
using System;
using Fdw.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Sample;

public sealed class ThingHealth
{
    [ServiceLocatorAllowed(""an interface demands an IServiceProvider"")]
    public bool CheckHealth(IServiceProvider services) => services.GetService<IThingProvider>() != null;
}

[ServiceLocatorAllowed(""the one scope runner (FDW-4639)"")]
public sealed class ScopeRunner
{
    private readonly IServiceScopeFactory _scopes;

    public ScopeRunner(IServiceScopeFactory scopes) { _scopes = scopes; }

    public void Run() { using var scope = _scopes.CreateScope(); }
}
", EnableRule);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Analyzer")]
    public Task AMarkWithAnEmptyReasonReportsAndAllowsNothing() => Run(@"
using System;
using Fdw.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Sample;

public sealed class ThingHealth
{
    [{|#0:ServiceLocatorAllowed("" "")|}]
    public bool CheckHealth(IServiceProvider {|#1:services|}) => {|#2:services.GetService<IThingProvider>()|} != null;
}
", EnableRule,
        At(0).WithMessage("[ServiceLocatorAllowed] on 'CheckHealth' has an empty reason. State why a runtime service locator is needed here; a mark without a reason allows nothing."),
        At(1).WithArguments("ThingHealth", "parameter 'services' of type IServiceProvider"),
        At(2));

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Analyzer")]
    public void TheRuleIsOffUntilEditorConfigTurnsItOn()
    {
        // What a rule ships as is its descriptors' IsEnabledByDefault. The test harness enables every descriptor of the analyzer it
        // runs, so running the analyzer with an empty editorconfig cannot show that the rule is off (FDW-4716).
        var supported = new RuntimeServiceLocatorAnalyzer().SupportedDiagnostics;

        Assert.NotEmpty(supported);
        Assert.All(supported, descriptor => Assert.False(descriptor.IsEnabledByDefault, descriptor.Id));
    }
}
