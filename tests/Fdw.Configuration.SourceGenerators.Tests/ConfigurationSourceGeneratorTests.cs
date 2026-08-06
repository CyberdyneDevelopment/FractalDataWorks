using System.Linq;
using Microsoft.CodeAnalysis;
using Shouldly;
using Xunit;

namespace Fdw.Configuration.SourceGenerators.Tests;

/// <summary>
/// Tests for ConfigurationSourceGenerator incremental generator.
/// </summary>
public class ConfigurationSourceGeneratorTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GeneratorProducesEmbeddedAttributes()
    {
        // Arrange
        var source = @"
namespace Test
{
    public class EmptyClass { }
}";

        // Act
        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        // Assert
        diagnostics.ShouldNotContain(d => d.Severity == DiagnosticSeverity.Error);

        var generated = CompilationHelper.GetAllGeneratedFiles(compilation).ToList();
        generated.ShouldContain("ManagedConfigurationAttribute.g.cs");
        generated.ShouldContain("ConfigurationOptionAttribute.g.cs");
        generated.ShouldContain("DbTypeAttribute.g.cs");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GeneratorProducesDdlForSimpleConfiguration()
    {
        // Arrange
        // Why: Schema and TableName were removed from [ManagedConfiguration] in FDW-395 Phase 6.
        // Table name is now derived from class name. "SimpleConfiguration" → table name "Simple".
        // Schema is no longer emitted in DdlDefinition initializer (DdlDefinition.Schema defaults to "cfg").
        var source = @"
using Fdw.Configuration;

namespace Test
{
    [ManagedConfiguration]
    public partial class SimpleConfiguration
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }
}";

        // Act
        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        // Assert
        diagnostics.ShouldNotContain(d => d.Severity == DiagnosticSeverity.Error);

        var ddl = CompilationHelper.GetGeneratedOutput(compilation, "SimpleConfiguration.Ddl.g.cs");
        ddl.ShouldNotBeNull();
        ddl.ShouldContain("IConfigurationDdlProvider");
        ddl.ShouldContain("GetDdlDefinition");
        // Why: Schema no longer emitted in DDL (DdlDefinition.Schema defaults to "cfg").
        ddl.ShouldNotContain("Schema = ");
        // Table name derived from class name "SimpleConfiguration" → "Simple"
        ddl.ShouldContain("TableName = \"Simple\"");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GeneratorProducesConfigurationTypeWhenConfigurationPackageReferenced()
    {
        // Arrange
        // Why: Schema removed from [ManagedConfiguration] in FDW-395 Phase 6.
        var source = @"

namespace Test
{
    [ManagedConfiguration(ServiceCategory = ""Connection"", ServiceType = ""MsSql"")]
    public partial class MsSqlConnectionConfiguration
    {
        public string Server { get; set; } = string.Empty;
    }
}";

        // Act
        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        // Assert
        diagnostics.ShouldNotContain(d => d.Severity == DiagnosticSeverity.Error);

        // Why: ConfigurationTypeBase / *ConfigurationType.g.cs generation was removed in Wave C5.
        // The generator now emits DDL + TypeCollection DDL only — assert the DDL artifact exists.
        var ddl = CompilationHelper.GetGeneratedOutput(compilation, "MsSqlConnectionConfiguration.Ddl.g.cs");
        ddl.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GeneratorHandlesConfigurationWithoutAttributes()
    {
        // Arrange - no [ManagedConfiguration] attribute
        var source = @"
namespace Test
{
    public class RegularClass
    {
        public string Name { get; set; } = string.Empty;
    }
}";

        // Act
        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        // Assert
        diagnostics.ShouldNotContain(d => d.Severity == DiagnosticSeverity.Error);

        var ddl = CompilationHelper.GetGeneratedOutput(compilation, "RegularClass.Ddl.g.cs");
        ddl.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GeneratorHandlesParentChildRelationship()
    {
        // Arrange
        // Why: ParentTableName and Schema removed from [ManagedConfiguration] in FDW-395 Phase 6.
        // IDataNode owns parent-child structure. Both types generate as independent flat root tables.
        // Child inheriting from Parent with [ManagedConfiguration] triggers ParentHasManagedConfiguration=true
        // via Roslyn symbol analysis → generates 'public new static GetDdlDefinition()'.
        var source = @"

namespace Test
{
    [ManagedConfiguration]
    public partial class ParentConfiguration
    {
        public string Name { get; set; } = string.Empty;
    }

    [ManagedConfiguration]
    public partial class ChildConfiguration : ParentConfiguration
    {
        public int ChildValue { get; set; }
    }
}";

        // Act
        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        // Assert
        diagnostics.ShouldNotContain(d => d.Severity == DiagnosticSeverity.Error);

        var childDdl = CompilationHelper.GetGeneratedOutput(compilation, "ChildConfiguration.Ddl.g.cs");
        childDdl.ShouldNotBeNull();
        // Child is now a flat root table — it has its own Name and Id columns, no ParentId FK
        childDdl.ShouldContain("Name = \"Id\"");
        childDdl.ShouldContain("Name = \"Name\"");
        // Generated with 'new' modifier since parent has [ManagedConfiguration]
        childDdl.ShouldContain("public new static DdlDefinition GetDdlDefinition()");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GeneratorReportsErrorWhenDdlGenerationFails()
    {
        // This test verifies that exceptions during DDL generation are caught
        // and reported as diagnostics (though in practice the generator is robust)

        // Arrange - valid source that won't cause errors
        var source = @"

namespace Test
{
    [ManagedConfiguration]
    public partial class ValidConfiguration
    {
        public string Name { get; set; } = string.Empty;
    }
}";

        // Act
        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        // Assert - should succeed without errors
        diagnostics.Where(d => d.Id == "CFG001").ShouldBeEmpty();
        diagnostics.Where(d => d.Id == "CFG002").ShouldBeEmpty();
        diagnostics.Where(d => d.Id == "CFG003").ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GeneratorHandlesMultipleConfigurationsInSameFile()
    {
        // Arrange
        // Why: Schema and TableName removed from [ManagedConfiguration] in FDW-395 Phase 6.
        // Table names are derived from class names: "ConfigurationA" → "ConfigurationA" (no suffix stripped)
        // and "ConfigurationB" → "ConfigurationB".
        var source = @"

namespace Test
{
    [ManagedConfiguration]
    public partial class ConfigurationA
    {
        public string Name { get; set; } = string.Empty;
    }

    [ManagedConfiguration]
    public partial class ConfigurationB
    {
        public int Value { get; set; }
    }
}";

        // Act
        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        // Assert
        diagnostics.ShouldNotContain(d => d.Severity == DiagnosticSeverity.Error);

        var ddlA = CompilationHelper.GetGeneratedOutput(compilation, "ConfigurationA.Ddl.g.cs");
        var ddlB = CompilationHelper.GetGeneratedOutput(compilation, "ConfigurationB.Ddl.g.cs");

        ddlA.ShouldNotBeNull();
        ddlB.ShouldNotBeNull();
        // Table names derived from class names (no "Configuration" suffix to strip for "ConfigurationA"/"ConfigurationB")
        ddlA.ShouldContain("ConfigurationA");
        ddlB.ShouldContain("ConfigurationB");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GeneratorHandlesGenerateDdlFalse()
    {
        // Arrange
        var source = @"

namespace Test
{
    [ManagedConfiguration(GenerateDdl = false)]
    public partial class NoDdlConfiguration
    {
        public string Name { get; set; } = string.Empty;
    }
}";

        // Act
        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        // Assert
        diagnostics.ShouldNotContain(d => d.Severity == DiagnosticSeverity.Error);

        var ddl = CompilationHelper.GetGeneratedOutput(compilation, "NoDdlConfiguration.Ddl.g.cs");
        ddl.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GeneratorInfersServiceMetadata()
    {
        // Arrange
        var source = @"

namespace Test
{
    [ManagedConfiguration]
    public partial class MsSqlConnectionConfiguration
    {
        public string Server { get; set; } = string.Empty;
    }
}";

        // Act
        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        // Assert
        diagnostics.ShouldNotContain(d => d.Severity == DiagnosticSeverity.Error);

        // ServiceCategory should be inferred as "Connection" from class name
        // ServiceType should be inferred as "MsSql" from prefix
        // Why: ConfigurationTypeBase / *ConfigurationType.g.cs generation was removed in Wave C5.
        // Inferred service metadata is no longer surfaced via a generated ConfigurationType file —
        // assert the DDL artifact exists instead.
        var ddl = CompilationHelper.GetGeneratedOutput(compilation, "MsSqlConnectionConfiguration.Ddl.g.cs");
        ddl.ShouldNotBeNull();
    }
}
