using System.Linq;
using Microsoft.CodeAnalysis;
using Shouldly;
using Xunit;

namespace Fdw.Registration.SourceGenerators.Tests;

// Why: ConfigurationTypeOptionAttribute and the ConfigurationTypes TypeCollection were deleted
// in Wave C5 (see ConfigurationSourceGenerator.cs:84-86). ConfigurationTypeModuleInitializerGenerator
// still exists but short-circuits when the attribute symbol is absent (the diagnostic file
// records this no-op so consumers can see what happened). The eight integration tests that
// previously exercised the [ConfigurationTypeOption] discovery path were deleted because the
// types they reference no longer compile; the diagnostic-file test below is the only behavior
// still worth verifying.
public class ConfigurationTypeModuleInitializerGeneratorTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void GeneratorAlwaysCreatesDiagnosticFile()
    {
        var source = @"
namespace Consumer;

public class Program
{
    public static void Main() { }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunConfigurationTypeGenerator(source);

        diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();

        var diagnostic = CompilationHelper.GetGeneratedOutput(compilation, "ConfigurationTypeModuleInitializer.Diagnostics.g.cs");
        diagnostic.ShouldNotBeNull();
        diagnostic.ShouldContain("ConfigurationTypeModuleInitializerGenerator");
    }
}
