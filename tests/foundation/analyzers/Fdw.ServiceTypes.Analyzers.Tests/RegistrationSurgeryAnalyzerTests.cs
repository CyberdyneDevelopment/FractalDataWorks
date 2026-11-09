using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Fdw.ServiceTypes.Analyzers.Tests;

/// <summary>
/// FDW062 (FDW-4641 C): a registration body never builds a second container, never edits another component's
/// registration, and no code declares an AddXxx extension on IServiceCollection or IHostApplicationBuilder.
/// </summary>
public class RegistrationSurgeryAnalyzerTests
{
    // Appended after each snippet, never before it: a snippet opens with its usings, which must precede every
    // namespace in the file (CS1529). No stub method starts with Add, so the stubs never report themselves.
    private const string Stubs = @"
namespace Microsoft.Extensions.DependencyInjection
{
    public class ServiceDescriptor
    {
        public System.Type ServiceType { get; } = typeof(object);
        public System.Func<System.IServiceProvider, object>? ImplementationFactory { get; }
        public object? ImplementationInstance { get; }
        public System.Type? ImplementationType { get; }
    }
    public interface IServiceCollection : System.Collections.Generic.IList<ServiceDescriptor> { }
    public static class ServiceCollectionContainerBuilderExtensions
    {
        public static System.IServiceProvider BuildServiceProvider(this IServiceCollection services) => default!;
    }
    public static class ServiceCollectionDescriptorExtensions
    {
        public static IServiceCollection Replace(this IServiceCollection services, ServiceDescriptor descriptor) => services;
        public static IServiceCollection RemoveAll<T>(this IServiceCollection services) => services;
        public static void TryAdd(this IServiceCollection services, ServiceDescriptor descriptor) { }
    }
}

namespace Microsoft.Extensions.Hosting
{
    public interface IHostApplicationBuilder { Microsoft.Extensions.DependencyInjection.IServiceCollection Services { get; } }
}

namespace Sample
{
    public interface IThingProvider { }
}
";

    private const string EnableRule = "dotnet_diagnostic.FDW062.severity = error";

    private static Task Run(string source, string editorConfig, params DiagnosticResult[] expected)
    {
        var test = new CSharpAnalyzerTest<RegistrationSurgeryAnalyzer, DefaultVerifier>
        {
            TestCode = source + Stubs,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };
        test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", "root = true\n\n[*]\n" + editorConfig + "\n"));
        test.ExpectedDiagnostics.AddRange(expected);
        return test.RunAsync(TestContext.Current.CancellationToken);
    }

    private static DiagnosticResult At(int location)
        => new DiagnosticResult(RegistrationSurgeryAnalyzer.DiagnosticId, DiagnosticSeverity.Error).WithLocation(location);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Analyzer")]
    public Task EachSurgeryShapeReports() => Run(@"
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sample;

public sealed class ThingType
{
    public void Register(IHostApplicationBuilder builder, ServiceDescriptor replacement)
    {
        using var built = {|#0:builder.Services.BuildServiceProvider()|} as System.IDisposable;
        var existing = builder.Services.Single(d => d.ServiceType == typeof(IThingProvider));
        {|#1:builder.Services.Remove(existing)|};
        var factory = {|#2:existing.ImplementationFactory|};
        var instance = {|#3:existing.ImplementationInstance|};
        {|#4:builder.Services.RemoveAll<IThingProvider>()|};
        {|#5:builder.Services.Replace(replacement)|};
        {|#6:builder.Services.RemoveAt(0)|};
        {|#7:builder.Services.Insert(0, replacement)|};
        {|#8:builder.Services.Clear()|};
    }
}
", EnableRule, At(0), At(1), At(2), At(3), At(4), At(5), At(6), At(7), At(8));

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Analyzer")]
    public Task AnAddExtensionOnTheServiceCollectionOrTheBuilderReports() => Run(@"
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public static class ThingServiceCollectionExtensions
{
    public static IServiceCollection {|#0:AddThings|}(this IServiceCollection services) => services;

    public static IHostApplicationBuilder {|#1:AddThingHost|}(this IHostApplicationBuilder builder) => builder;
}
", EnableRule,
        At(0).WithArguments("ThingServiceCollectionExtensions", "declares the extension 'AddThings' on IServiceCollection"),
        At(1).WithArguments("ThingServiceCollectionExtensions", "declares the extension 'AddThingHost' on IHostApplicationBuilder"));

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Analyzer")]
    public Task OrdinaryRegistrationIsSilent() => Run(@"
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sample;

public sealed class ThingType
{
    public void Register(IHostApplicationBuilder builder, ServiceDescriptor descriptor)
    {
        builder.Services.TryAdd(descriptor);
        var registered = builder.Services.Any(d => d.ServiceType == typeof(IThingProvider));
        var names = new List<string> { ""a"" };
        names.Remove(""a"");
        names.Clear();
    }

    public static void AddItem(IServiceCollection services, ServiceDescriptor descriptor) => services.TryAdd(descriptor);
}

public static class ThingWiring
{
    public static IServiceCollection Wire(this IServiceCollection services) => services;

    public static List<string> AddName(this List<string> names) => names;
}
", EnableRule);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Analyzer")]
    public void TheRuleIsOffUntilEditorConfigTurnsItOn()
    {
        // What a rule ships as is its descriptors' IsEnabledByDefault. The test harness enables every descriptor of the analyzer it
        // runs, so running the analyzer with an empty editorconfig cannot show that the rule is off (FDW-4716).
        var supported = new RegistrationSurgeryAnalyzer().SupportedDiagnostics;

        Assert.NotEmpty(supported);
        Assert.All(supported, descriptor => Assert.False(descriptor.IsEnabledByDefault, descriptor.Id));
    }
}
