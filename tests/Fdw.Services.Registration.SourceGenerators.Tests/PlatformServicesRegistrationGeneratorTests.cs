using System.Linq;
using Microsoft.CodeAnalysis;
using Shouldly;
using Xunit;

namespace Fdw.Services.Registration.SourceGenerators.Tests;

[Trait("Priority", "P1")]
[Trait("Category", "CoreFramework")]
public sealed class PlatformServicesRegistrationGeneratorTests
{
    private const string ValidCollectionSource = """
        using System;
        using Microsoft.Extensions.DependencyInjection;
        using Microsoft.Extensions.Hosting;
        using Microsoft.Extensions.Logging;
        using Fdw.Collections;

        namespace TestDomain;

        [ServiceTypeCollection(typeof(object), typeof(object), typeof(WidgetTypes), ServiceCategory = "Widget")]
        public static class WidgetTypes
        {
            public static TBuilder Configure<TBuilder>(TBuilder builder, ILoggerFactory? loggerFactory = null)
                where TBuilder : IHostApplicationBuilder
                => builder;

            public static void Register(IServiceCollection services, ILoggerFactory? loggerFactory = null) { }

            public static void Initialize(IServiceProvider services, ILoggerFactory? loggerFactory = null) { }
        }
        """;

    // Why this is the security-relevant regression guard: this generator's ONLY job is wiring each
    // discovered domain's own Configure/Register/Initialize method groups into a descriptor — it must
    // NEVER wrap, resolve a service, or otherwise inject domain-specific behavior (e.g. an authentication-
    // context boot-elevation scope) around what a domain's Initialize does. A prior version of this
    // generator bracketed every emitted Initialize in a SystemAuthenticationContextScope whenever the
    // compilation could see the auth abstractions — that broke any host with no auth-context-consuming
    // connection layer (e.g. a FileSystem-only UI host) with "No service for type
    // 'IAuthenticationContextAccessor'", since the accessor is registered only by MsSqlConnectionType. A
    // domain that genuinely needs something extra around its own Initialize does that itself via its own
    // Initialization(customFunc) override — never here, since baking it into every host's registration
    // file forces it on hosts that never need it. This test asserts the invariant holds BOTH when the
    // auth abstractions are and are not visible to the compilation, so the assertion cannot pass by
    // accident of what the generator can or cannot see.
    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    [InlineData(false)]
    [InlineData(true)]
    public void NeverWrapsOrElevatesInitializeRegardlessOfAuthAbstractionsVisibility(bool authAbstractionsVisible)
    {
        var (compilation, diagnostics) = CompilationHelper.RunGenerator(
            ValidCollectionSource,
            additionalReferences: authAbstractionsVisible
                ?
                [
                    MetadataReference.CreateFromFile(
                        typeof(Fdw.Services.Authentication.Abstractions.Security.SystemAuthenticationContextScope).Assembly.Location),
                ]
                : []);

        diagnostics.ShouldBeEmpty();
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "PlatformServicesRegistration.g.cs");
        generated.ShouldNotBeNull();

        // Initialize is always the bare method group — no wrapping lambda, no service resolution.
        generated.ShouldContain("TestDomain.WidgetTypes.Initialize),");
        generated.ShouldNotContain("SystemAuthenticationContextScope");
        generated.ShouldNotContain("IAuthenticationContextAccessor");
        generated.ShouldNotContain("GetRequiredService");
    }

    [Fact]
    public void EmitsModuleInitializerAndExtensionForValidCollection()
    {
        var (compilation, diagnostics) = CompilationHelper.RunGenerator(ValidCollectionSource);

        diagnostics.ShouldBeEmpty();

        var generated = compilation.SyntaxTrees
            .Select(t => t.GetText().ToString())
            .FirstOrDefault(t => t.Contains("PlatformServicesRegistration"));

        generated.ShouldNotBeNull();
        generated.ShouldContain("[ModuleInitializer]");
        generated.ShouldContain("internal static PlatformServiceEntry? _widgetEntry;");
        generated.ShouldContain("_widgetEntry = PlatformServices.Add(");
        generated.ShouldContain("\"Widget\"");
        generated.ShouldContain("typeof(TestDomain.WidgetTypes)");
        generated.ShouldContain("TestDomain.WidgetTypes.Configure,");
        generated.ShouldContain("TestDomain.WidgetTypes.Register,");
        generated.ShouldContain("TestDomain.WidgetTypes.Initialize),");
        // Why 10: the default Group when [ServiceTypeCollection] doesn't specify one — matches the
        // attribute's own default and "everything else defaults to 10" in the canonical layer scheme.
        generated.ShouldContain("10);");
        // Why non-nullable: the module initializer always assigns the backing field before any read,
        // so the dot-walk property fails loud (?? throw) rather than handing back a nullable.
        generated.ShouldContain("public static PlatformServiceEntry Widget");
        generated.ShouldContain("=> PlatformServicesRegistration._widgetEntry");
        // No name-based lookup surface — dot-walk only (no actual ByName(...) call emitted).
        generated.ShouldNotContain(".ByName(");
    }

    [Fact]
    public void EmitsManualTrueArgumentWhenAttributeDeclaresManual()
    {
        const string source = """
            using System;
            using Microsoft.Extensions.DependencyInjection;
            using Microsoft.Extensions.Hosting;
            using Microsoft.Extensions.Logging;
            using Fdw.Collections;

            namespace TestDomain;

            [ServiceTypeCollection(typeof(object), typeof(object), typeof(ChosenTypes), ServiceCategory = "Chosen", Manual = true)]
            public static class ChosenTypes
            {
                public static TBuilder Configure<TBuilder>(TBuilder builder, ILoggerFactory? loggerFactory = null)
                    where TBuilder : IHostApplicationBuilder
                    => builder;

                public static void Register(IServiceCollection services, ILoggerFactory? loggerFactory = null) { }

                public static void Initialize(IServiceProvider services, ILoggerFactory? loggerFactory = null) { }
            }
            """;

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        diagnostics.ShouldBeEmpty();

        var generated = compilation.SyntaxTrees
            .Select(t => t.GetText().ToString())
            .FirstOrDefault(t => t.Contains("PlatformServicesRegistration"));

        generated.ShouldNotBeNull();
        generated.ShouldContain("_chosenEntry = PlatformServices.Add(");
        generated.ShouldContain("manual: true);");
    }

    [Fact]
    public void EmitsExplicitGroupValueWhenAttributeDeclaresGroup()
    {
        const string source = """
            using System;
            using Microsoft.Extensions.DependencyInjection;
            using Microsoft.Extensions.Hosting;
            using Microsoft.Extensions.Logging;
            using Fdw.Collections;

            namespace TestDomain;

            [ServiceTypeCollection(typeof(object), typeof(object), typeof(FoundationTypes), ServiceCategory = "Foundation", Group = 1)]
            public static class FoundationTypes
            {
                public static TBuilder Configure<TBuilder>(TBuilder builder, ILoggerFactory? loggerFactory = null)
                    where TBuilder : IHostApplicationBuilder
                    => builder;

                public static void Register(IServiceCollection services, ILoggerFactory? loggerFactory = null) { }

                public static void Initialize(IServiceProvider services, ILoggerFactory? loggerFactory = null) { }
            }
            """;

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        diagnostics.ShouldBeEmpty();

        var generated = compilation.SyntaxTrees
            .Select(t => t.GetText().ToString())
            .FirstOrDefault(t => t.Contains("PlatformServicesRegistration"));

        generated.ShouldNotBeNull();
        generated.ShouldContain("_foundationEntry = PlatformServices.Add(");
        generated.ShouldContain("1);");
    }

    [Fact]
    public void EmitsRegistrationUnconditionallyEvenWhenPhaseMethodsAreMissing()
    {
        // Why: the generator no longer gates on the three-phase shape (PLATSVC001 is gone) — the
        // Fdw.ServiceTypes.Analyzers FDW024 ServiceTypeCollectionPhaseMethodsAnalyzer now enforces that
        // shape as a build ERROR, so by the time this generator runs every discovered class is
        // guaranteed to have it. This test only exercises the generator's own compilation (without the
        // ServiceTypes analyzer wired in), so it must emit the registration unconditionally rather than
        // silently skip the type.
        const string source = """
            using Fdw.Collections;

            namespace TestDomain;

            [ServiceTypeCollection(typeof(object), typeof(object), typeof(BrokenTypes), ServiceCategory = "Broken")]
            public static class BrokenTypes
            {
                // Missing Configure/Register/Initialize entirely.
            }
            """;

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        diagnostics.ShouldBeEmpty();

        var generated = compilation.SyntaxTrees
            .Select(t => t.GetText().ToString())
            .FirstOrDefault(t => t.Contains("PlatformServicesRegistration"));

        generated.ShouldNotBeNull();
        generated.ShouldContain("BrokenTypes");
    }

    [Fact]
    public void EmitsNothingForLibraryOutputKind()
    {
        var (compilation, _) = CompilationHelper.RunGenerator(ValidCollectionSource, outputKind: OutputKind.DynamicallyLinkedLibrary);

        var generated = compilation.SyntaxTrees
            .Select(t => t.GetText().ToString())
            .FirstOrDefault(t => t.Contains("PlatformServicesRegistration"));

        generated.ShouldBeNull();
    }
}
