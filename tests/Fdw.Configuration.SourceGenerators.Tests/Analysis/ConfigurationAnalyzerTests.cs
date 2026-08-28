using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Fdw.Configuration.SourceGenerators.Analysis;
using Shouldly;
using Xunit;

namespace Fdw.Configuration.SourceGenerators.Tests.Analysis;

public class ConfigurationAnalyzerTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void AnalyzeExtractsBasicMetadata()
    {
        // Arrange
        var source = @"
using Fdw.Configuration;

namespace Test
{
    [ManagedConfiguration(DisplayName = ""Test Display"", Description = ""Test desc"")]
    public partial class TestConfiguration
    {
        public string Name { get; set; } = string.Empty;
    }
}";

        // Run generator to embed attribute definition
        var (compilation, _) = CompilationHelper.RunGenerator(source);
        var classSymbol = compilation.GetTypeByMetadataName("Test.TestConfiguration");
        var attribute = classSymbol!.GetAttributes().First();

        // Act
        var model = ConfigurationAnalyzer.AnalyzeWithAttribute(classSymbol, attribute);

        // Assert
        model.ShouldNotBeNull();
        model.Namespace.ShouldBe("Test");
        model.ClassName.ShouldBe("TestConfiguration");
        model.Schema.ShouldBe("cfg");
        model.TableName.ShouldBeNull();
        model.GetEffectiveTableName().ShouldBe("Test");
        model.DisplayName.ShouldBe("Test Display");
        model.Description.ShouldBe("Test desc");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void AnalyzeIgnoresRemovedStructuralAttributeArgs()
    {
        var source = @"

namespace Test
{
    [ManagedConfiguration(ServiceCategory = ""Test"")]
    public partial class ChildConfiguration
    {
        public int Value { get; set; }
    }
}";

        var (compilation, _) = CompilationHelper.RunGenerator(source);
        var classSymbol = compilation.GetTypeByMetadataName("Test.ChildConfiguration");
        var attribute = classSymbol!.GetAttributes().First();

        // Act
        var model = ConfigurationAnalyzer.AnalyzeWithAttribute(classSymbol, attribute);

        // Assert: structural fields are null/default — not set from attribute
        model.ParentTableName.ShouldBeNull();
        model.ParentSchema.ShouldBeNull();
        // Schema always defaults to "cfg" in ConfigurationModel
        model.Schema.ShouldBe("cfg");
        model.ServiceCategory.ShouldBe("Test");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void AnalyzeInfersServiceCategoryFromClassName()
    {
        // Arrange - class name ends with "ConnectionConfiguration"
        var source = @"

namespace Test
{
    [ManagedConfiguration]
    public partial class MsSqlConnectionConfiguration
    {
        public string Server { get; set; } = string.Empty;
    }
}";

        var (compilation, _) = CompilationHelper.RunGenerator(source);
        var classSymbol = compilation.GetTypeByMetadataName("Test.MsSqlConnectionConfiguration");
        var attribute = classSymbol!.GetAttributes().First();

        // Act
        var model = ConfigurationAnalyzer.AnalyzeWithAttribute(classSymbol, attribute);

        // Assert
        model.ServiceCategory.ShouldBe("Connection");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void AnalyzeInfersServiceTypeFromPrefix()
    {
        // Arrange
        var source = @"

namespace Test
{
    [ManagedConfiguration]
    public partial class PostgreSqlConnectionConfiguration
    {
        public string Server { get; set; } = string.Empty;
    }
}";

        var (compilation, _) = CompilationHelper.RunGenerator(source);
        var classSymbol = compilation.GetTypeByMetadataName("Test.PostgreSqlConnectionConfiguration");
        var attribute = classSymbol!.GetAttributes().First();

        // Act
        var model = ConfigurationAnalyzer.AnalyzeWithAttribute(classSymbol, attribute);

        // Assert
        model.ServiceType.ShouldBe("PostgreSql");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void AnalyzeUsesExplicitServiceMetadata()
    {
        // Arrange
        var source = @"

namespace Test
{
    [ManagedConfiguration(ServiceCategory = ""Storage"", ServiceType = ""Blob"")]
    public partial class CustomConfiguration
    {
        public string Name { get; set; } = string.Empty;
    }
}";

        var (compilation, _) = CompilationHelper.RunGenerator(source);
        var classSymbol = compilation.GetTypeByMetadataName("Test.CustomConfiguration");
        var attribute = classSymbol!.GetAttributes().First();

        // Act
        var model = ConfigurationAnalyzer.AnalyzeWithAttribute(classSymbol, attribute);

        // Assert
        model.ServiceCategory.ShouldBe("Storage");
        model.ServiceType.ShouldBe("Blob");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void AnalyzeExtractsGenerationFlags()
    {
        // Arrange
        var source = @"

namespace Test
{
    [ManagedConfiguration(GenerateDdl = true, GenerateValidator = false, GenerateUi = false)]
    public partial class TestConfiguration
    {
        public string Name { get; set; } = string.Empty;
    }
}";

        var (compilation, _) = CompilationHelper.RunGenerator(source);
        var classSymbol = compilation.GetTypeByMetadataName("Test.TestConfiguration");
        var attribute = classSymbol!.GetAttributes().First();

        // Act
        var model = ConfigurationAnalyzer.AnalyzeWithAttribute(classSymbol, attribute);

        // Assert
        model.GenerateDdl.ShouldBeTrue();
        model.GenerateValidator.ShouldBeFalse();
        model.GenerateUi.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void AnalyzeHandlesOnDeleteAndDatabaseProvider()
    {
        // Arrange
        var source = @"

namespace Test
{
    [ManagedConfiguration(OnDelete = ""SetNull"", DatabaseProvider = ""PostgreSql"")]
    public partial class TestConfiguration
    {
        public string Name { get; set; } = string.Empty;
    }
}";

        var (compilation, _) = CompilationHelper.RunGenerator(source);
        var classSymbol = compilation.GetTypeByMetadataName("Test.TestConfiguration");
        var attribute = classSymbol!.GetAttributes().First();

        // Act
        var model = ConfigurationAnalyzer.AnalyzeWithAttribute(classSymbol, attribute);

        // Assert
        model.OnDelete.ShouldBe("SetNull");
        model.DatabaseProvider.ShouldBe("PostgreSql");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void AnalyzeDetectsPublicPropertiesOnly()
    {
        // Arrange
        var source = @"

namespace Test
{
    [ManagedConfiguration]
    public partial class TestConfiguration
    {
        public string PublicProp { get; set; } = string.Empty;
        private string PrivateProp { get; set; } = string.Empty;
        internal string InternalProp { get; set; } = string.Empty;
    }
}";

        var (compilation, _) = CompilationHelper.RunGenerator(source);
        var classSymbol = compilation.GetTypeByMetadataName("Test.TestConfiguration");
        var attribute = classSymbol!.GetAttributes().First();

        // Act
        var model = ConfigurationAnalyzer.AnalyzeWithAttribute(classSymbol, attribute);

        // Assert
        model.Properties.Count.ShouldBe(1);
        model.Properties[0].PropertyName.ShouldBe("PublicProp");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void AnalyzeIgnoresPropertiesWithoutSetter()
    {
        // Arrange
        var source = @"

namespace Test
{
    [ManagedConfiguration]
    public partial class TestConfiguration
    {
        public string ReadWrite { get; set; } = string.Empty;
        public string ReadOnly { get; } = string.Empty;
    }
}";

        var (compilation, _) = CompilationHelper.RunGenerator(source);
        var classSymbol = compilation.GetTypeByMetadataName("Test.TestConfiguration");
        var attribute = classSymbol!.GetAttributes().First();

        // Act
        var model = ConfigurationAnalyzer.AnalyzeWithAttribute(classSymbol, attribute);

        // Assert
        model.Properties.Count.ShouldBe(1);
        model.Properties[0].PropertyName.ShouldBe("ReadWrite");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void AnalyzeDetectsParentHasManagedConfigurationAttribute()
    {
        // Arrange
        var source = @"

namespace Test
{
    [ManagedConfiguration(TableName = ""Parent"")]
    public partial class ParentConfiguration
    {
        public string Name { get; set; } = string.Empty;
    }

    [ManagedConfiguration(TableName = ""Child"")]
    public partial class ChildConfiguration : ParentConfiguration
    {
        public int Value { get; set; }
    }
}";

        var (compilation, _) = CompilationHelper.RunGenerator(source);
        var childSymbol = compilation.GetTypeByMetadataName("Test.ChildConfiguration");
        var attribute = childSymbol!.GetAttributes().First();

        // Act
        var model = ConfigurationAnalyzer.AnalyzeWithAttribute(childSymbol, attribute);

        // Assert
        model.ParentHasManagedConfiguration.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void AnalyzeHandlesParentWithoutManagedConfigurationAttribute()
    {
        // Arrange
        var source = @"

namespace Test
{
    public class BaseClass
    {
        public string Name { get; set; } = string.Empty;
    }

    [ManagedConfiguration(ServiceCategory = ""Test"")]
    public partial class DerivedConfiguration : BaseClass
    {
        public int Value { get; set; }
    }
}";

        var (compilation, _) = CompilationHelper.RunGenerator(source);
        var derivedSymbol = compilation.GetTypeByMetadataName("Test.DerivedConfiguration");
        var attribute = derivedSymbol!.GetAttributes().First();

        // Act
        var model = ConfigurationAnalyzer.AnalyzeWithAttribute(derivedSymbol, attribute);

        // Assert
        model.ParentHasManagedConfiguration.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void AnalyzeHandlesParentClassWithManagedConfigurationAttributeDetection()
    {
        var source = @"

namespace Test
{
    public class PlainBase
    {
        public int BaseValue { get; set; }
    }

    [ManagedConfiguration(ServiceCategory = ""Test"")]
    public partial class ChildConfiguration : PlainBase
    {
        public int Value { get; set; }
    }
}";

        var (compilation, _) = CompilationHelper.RunGenerator(source);
        var classSymbol = compilation.GetTypeByMetadataName("Test.ChildConfiguration");
        var attribute = classSymbol!.GetAttributes().First();

        // Act
        var model = ConfigurationAnalyzer.AnalyzeWithAttribute(classSymbol, attribute);

        // Assert: base class has no [ManagedConfiguration], so ParentHasManagedConfiguration = false
        model.ParentHasManagedConfiguration.ShouldBeFalse();
        model.ExplicitParentForeignKeyColumn.ShouldBeNull();
    }
}
