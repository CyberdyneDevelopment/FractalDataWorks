using System.Linq;
using Microsoft.CodeAnalysis;
using Shouldly;
using Xunit;

namespace Fdw.Registration.SourceGenerators.Tests;

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
