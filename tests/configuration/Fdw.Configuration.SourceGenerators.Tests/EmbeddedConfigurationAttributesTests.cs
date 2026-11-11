using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Shouldly;
using Xunit;

namespace Fdw.Configuration.SourceGenerators.Tests;

/// <summary>
/// Tests that the attributes the configuration generator emits stay inside their own assembly (FDW-5184).
/// </summary>
/// <remarks>
/// Every project that uses the generator gets its own internal copy of ManagedConfigurationAttribute,
/// ConfigurationOptionAttribute and DbTypeAttribute. When one such project grants InternalsVisibleTo to another,
/// a plain internal copy becomes visible there too and collides with the local one (CS0436). The copies are marked
/// [Microsoft.CodeAnalysis.Embedded], which keeps a type out of every other assembly, even through InternalsVisibleTo.
/// </remarks>
public sealed class EmbeddedConfigurationAttributesTests
{
    private static readonly string[] AttributeFiles =
        ["ManagedConfigurationAttribute.g.cs", "ConfigurationOptionAttribute.g.cs", "DbTypeAttribute.g.cs"];

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void EveryGeneratedAttributeIsEmbeddedAndTheProjectCompilesClean()
    {
        var (compilation, diagnostics) = CompilationHelper.RunGenerator("namespace Test { public class EmptyClass { } }");

        diagnostics.ShouldNotContain(d => d.Severity == DiagnosticSeverity.Error);
        foreach (var file in AttributeFiles)
        {
            CompilationHelper.GetGeneratedOutput(compilation, file).ShouldNotBeNull(file)
                .ShouldContain("[global::Microsoft.CodeAnalysis.EmbeddedAttribute]", Case.Sensitive, file);
        }

        compilation.GetTypeByMetadataName("Microsoft.CodeAnalysis.EmbeddedAttribute").ShouldNotBeNull();
        compilation.GetDiagnostics(TestContext.Current.CancellationToken)
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void AnInternalsVisibleToGrantDoesNotBringInASecondCopyOfTheAttributes()
    {
        var producer = Producer();

        var (consumer, diagnostics) = CompilationHelper.RunGenerator(
            """
            namespace Consumer
            {
                [ManagedConfiguration]
                public partial class ParentConfiguration
                {
                    public string Label { get; set; } = string.Empty;
                    public Producer.ChildConfiguration? Child { get; set; }
                }
            }
            """,
            [producer],
            "Consumer");

        diagnostics.ShouldNotContain(d => d.Severity == DiagnosticSeverity.Error);
        consumer.GetDiagnostics(TestContext.Current.CancellationToken).ShouldNotContain(d => d.Id == "CS0436");

        // One copy only, the consumer's own: a second visible copy would make the lookup ambiguous (null).
        consumer.GetTypeByMetadataName("Fdw.Configuration.ManagedConfigurationAttribute").ShouldNotBeNull()
            .ContainingAssembly.Name.ShouldBe("Consumer");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void AManagedConfigurationTypeFromAnotherAssemblyIsStillANavigationPropertyNotAColumn()
    {
        var producer = Producer();

        var (consumer, diagnostics) = CompilationHelper.RunGenerator(
            """
            namespace Consumer
            {
                [ManagedConfiguration]
                public partial class ParentConfiguration
                {
                    public string Label { get; set; } = string.Empty;
                    public Producer.ChildConfiguration? Child { get; set; }
                }
            }
            """,
            [producer],
            "Consumer");

        diagnostics.ShouldNotContain(d => d.Severity == DiagnosticSeverity.Error);
        var ddl = CompilationHelper.GetGeneratedOutput(consumer, "ParentConfiguration.Ddl.g.cs").ShouldNotBeNull();
        ddl.ShouldContain("Name = \"Label\"");
        ddl.ShouldNotContain("Name = \"Child\"");
    }

    // An assembly that uses the generator and grants InternalsVisibleTo to "Consumer", emitted to a real image so the
    // consumer reads it as metadata, the way a referenced project or package is read.
    private static PortableExecutableReference Producer()
    {
        var (producer, diagnostics) = CompilationHelper.RunGenerator(
            """
            [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Consumer")]

            namespace Producer
            {
                [ManagedConfiguration(GenerateDdl = false)]
                public partial class ChildConfiguration
                {
                    public int Value { get; set; }
                }
            }
            """,
            assemblyName: "Producer");
        diagnostics.ShouldNotContain(d => d.Severity == DiagnosticSeverity.Error);

        using var image = new MemoryStream();
        var emitted = producer.Emit(image, cancellationToken: TestContext.Current.CancellationToken);
        emitted.Success.ShouldBeTrue(string.Join("; ", emitted.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)));

        return MetadataReference.CreateFromImage(image.ToArray());
    }
}
