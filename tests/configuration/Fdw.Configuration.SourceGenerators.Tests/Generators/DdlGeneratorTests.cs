using System.Linq;
using Microsoft.CodeAnalysis;
using Shouldly;
using Xunit;

namespace Fdw.Configuration.SourceGenerators.Tests.Generators;

public class DdlGeneratorTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GenerateDdlCreatesRowIdPrimaryKey()
    {
        // Arrange
        var source = @"
using Fdw.Configuration;

namespace Test
{
    [ManagedConfiguration]
    public partial class TestConfiguration
    {
        public string Name { get; set; } = string.Empty;
    }
}";

        // Act
        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        // Assert
        var ddl = CompilationHelper.GetGeneratedOutput(compilation, "TestConfiguration.Ddl.g.cs");
        ddl.ShouldNotBeNull();
        ddl.ShouldContain("Name = \"RowId\"");
        ddl.ShouldNotContain("IsPrimaryKey");
        ddl.ShouldContain("DefaultValue = \"NEWID()\"");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GenerateDdlCreatesIdColumnForRootTables()
    {
        // Arrange
        var source = @"

namespace Test
{
    [ManagedConfiguration]
    public partial class RootConfiguration
    {
        public string Name { get; set; } = string.Empty;
    }
}";

        // Act
        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        // Assert
        var ddl = CompilationHelper.GetGeneratedOutput(compilation, "RootConfiguration.Ddl.g.cs");
        ddl.ShouldNotBeNull();
        ddl.ShouldContain("Name = \"Id\"");
        ddl.ShouldContain("SqlType = \"uniqueidentifier\"");
        ddl.ShouldNotContain("IsPrimaryKey");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GenerateDdlCreatesNameColumnForRootTables()
    {
        // Arrange
        var source = @"

namespace Test
{
    [ManagedConfiguration]
    public partial class RootConfiguration
    {
        public string Name { get; set; } = string.Empty;
    }
}";

        // Act
        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        // Assert
        var ddl = CompilationHelper.GetGeneratedOutput(compilation, "RootConfiguration.Ddl.g.cs");
        ddl.ShouldNotBeNull();
        ddl.ShouldContain("Name = \"Name\"");
        ddl.ShouldContain("SqlType = \"varchar\"");
        ddl.ShouldContain("MaxLength = 256");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GenerateDdlAllTablesAreRootTables()
    {
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
        public int Value { get; set; }
    }
}";

        // Act
        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        // Assert: both types get Id and Name columns (all are root tables)
        var parentDdl = CompilationHelper.GetGeneratedOutput(compilation, "ParentConfiguration.Ddl.g.cs");
        var childDdl = CompilationHelper.GetGeneratedOutput(compilation, "ChildConfiguration.Ddl.g.cs");
        parentDdl.ShouldNotBeNull();
        childDdl.ShouldNotBeNull();
        parentDdl.ShouldContain("Name = \"Id\"");
        parentDdl.ShouldContain("Name = \"Name\"");
        // Child also gets Id and Name — it's a flat root table, not a child table
        childDdl.ShouldContain("Name = \"Id\"");
        childDdl.ShouldContain("Name = \"Name\"");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GenerateDdlCreatesAuditColumns()
    {
        // Arrange
        var source = @"

namespace Test
{
    [ManagedConfiguration]
    public partial class TestConfiguration
    {
        public string Name { get; set; } = string.Empty;
    }
}";

        // Act
        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        // Assert
        var ddl = CompilationHelper.GetGeneratedOutput(compilation, "TestConfiguration.Ddl.g.cs");
        ddl.ShouldNotBeNull();

        // Check all 8 audit columns
        ddl.ShouldContain("Name = \"IsCurrent\"");
        ddl.ShouldContain("Name = \"IsDeleted\"");
        ddl.ShouldContain("Name = \"CreateDate\"");
        ddl.ShouldContain("Name = \"CreateBy\"");
        ddl.ShouldContain("Name = \"CreateByUser\"");
        ddl.ShouldContain("Name = \"ModifyDate\"");
        ddl.ShouldContain("Name = \"ModifyBy\"");
        ddl.ShouldContain("Name = \"ModifyOnBehalfOf\"");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GenerateDdlCreatesFilteredUniqueIndexes()
    {
        // Arrange — use class name "TestConfiguration" which generates table name "Test"
        var source = @"

namespace Test
{
    [ManagedConfiguration]
    public partial class TestConfiguration
    {
        public string Name { get; set; } = string.Empty;
    }
}";

        // Act
        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        // Assert — table name is "Test" (class name "TestConfiguration" strips "Configuration" suffix)
        var ddl = CompilationHelper.GetGeneratedOutput(compilation, "TestConfiguration.Ddl.g.cs");
        ddl.ShouldNotBeNull();
        ddl.ShouldContain("Name = \"UX_Test_Id_Current\"");
        ddl.ShouldContain("Name = \"UX_Test_Name_Current\"");
        ddl.ShouldContain("FilterPredicate = \"IsCurrent = 1\"");
        ddl.ShouldContain("FilterPredicate = \"IsCurrent = 1 AND IsDeleted = 0\"");
        ddl.ShouldContain("IsUnique = true");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GenerateDdlCreatesStandardIndexes()
    {
        // Arrange — use class name "TestConfiguration" which generates table name "Test"
        var source = @"

namespace Test
{
    [ManagedConfiguration]
    public partial class TestConfiguration
    {
        public string Name { get; set; } = string.Empty;
    }
}";

        // Act
        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        // Assert — table name is "Test" (derived from class name)
        var ddl = CompilationHelper.GetGeneratedOutput(compilation, "TestConfiguration.Ddl.g.cs");
        ddl.ShouldNotBeNull();
        ddl.ShouldContain("Name = \"IX_Test_Id\"");
        ddl.ShouldContain("Name = \"IX_Test_IsCurrent\"");
        ddl.ShouldContain("Name = \"IX_Test_IsDeleted\"");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GenerateDdlImplementsIConfigurationDdlProvider()
    {
        // Arrange
        var source = @"

namespace Test
{
    [ManagedConfiguration]
    public partial class TestConfiguration
    {
        public string Name { get; set; } = string.Empty;
    }
}";

        // Act
        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        // Assert
        var ddl = CompilationHelper.GetGeneratedOutput(compilation, "TestConfiguration.Ddl.g.cs");
        ddl.ShouldNotBeNull();
        ddl.ShouldContain("partial class TestConfiguration : IConfigurationDdlProvider");
        ddl.ShouldContain("public static DdlDefinition GetDdlDefinition()");
        ddl.ShouldContain("DdlDefinition IConfigurationDdlProvider.GetDefinition() => GetDdlDefinition()");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GenerateDdlUsesNewKeywordWhenParentHasManagedConfiguration()
    {
        // Arrange
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
        public int Value { get; set; }
    }
}";

        // Act
        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        // Assert
        var ddl = CompilationHelper.GetGeneratedOutput(compilation, "ChildConfiguration.Ddl.g.cs");
        ddl.ShouldNotBeNull();
        ddl.ShouldContain("public new static DdlDefinition GetDdlDefinition()");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GenerateDdlDoesNotUseNewKeywordWhenParentDoesNotHaveManagedConfiguration()
    {
        // Arrange
        var source = @"

namespace Test
{
    public class BaseClass
    {
        public string Name { get; set; } = string.Empty;
    }

    [ManagedConfiguration]
    public partial class DerivedConfiguration : BaseClass
    {
        public int Value { get; set; }
    }
}";

        // Act
        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        // Assert
        var ddl = CompilationHelper.GetGeneratedOutput(compilation, "DerivedConfiguration.Ddl.g.cs");
        ddl.ShouldNotBeNull();
        ddl.ShouldContain("public static DdlDefinition GetDdlDefinition()");
        ddl.ShouldNotContain("public new static DdlDefinition GetDdlDefinition()");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GenerateDdlMapsPropertiesToColumns()
    {
        // Arrange
        var source = @"

namespace Test
{
    [ManagedConfiguration]
    public partial class TestConfiguration
    {
        public string Name { get; set; } = string.Empty;
        public string Server { get; set; } = string.Empty;
        public int Port { get; set; }
        public bool IsEnabled { get; set; }
    }
}";

        // Act
        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        // Assert
        var ddl = CompilationHelper.GetGeneratedOutput(compilation, "TestConfiguration.Ddl.g.cs");
        ddl.ShouldNotBeNull();
        ddl.ShouldContain("Name = \"Server\"");
        ddl.ShouldContain("Name = \"Port\"");
        ddl.ShouldContain("Name = \"IsEnabled\"");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GenerateDdlHandlesExplicitTableName()
    {
        var source = @"

namespace Test
{
    [ManagedConfiguration]
    public partial class TestConfiguration
    {
        public string Name { get; set; } = string.Empty;
    }
}";

        // Act
        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        // Assert: table name is derived from class name "TestConfiguration" → "Test"
        var ddl = CompilationHelper.GetGeneratedOutput(compilation, "TestConfiguration.Ddl.g.cs");
        ddl.ShouldNotBeNull();
        ddl.ShouldContain("TableName = \"Test\"");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GenerateDdlInfersTableNameFromClassName()
    {
        // Arrange
        var source = @"

namespace Test
{
    [ManagedConfiguration]
    public partial class MsSqlConnectionConfiguration
    {
        public string Name { get; set; } = string.Empty;
    }
}";

        // Act
        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        // Assert
        var ddl = CompilationHelper.GetGeneratedOutput(compilation, "MsSqlConnectionConfiguration.Ddl.g.cs");
        ddl.ShouldNotBeNull();
        ddl.ShouldContain("TableName = \"MsSqlConnection\"");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GenerateDdlUsesSpecifiedSchema()
    {
        var source = @"

namespace Test
{
    [ManagedConfiguration]
    public partial class TestConfiguration
    {
        public string Name { get; set; } = string.Empty;
    }
}";

        // Act
        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        // Assert: DDL does NOT emit a Schema initializer — DdlDefinition.Schema defaults to "cfg"
        var ddl = CompilationHelper.GetGeneratedOutput(compilation, "TestConfiguration.Ddl.g.cs");
        ddl.ShouldNotBeNull();
        ddl.ShouldNotContain("Schema = ");
        ddl.ShouldContain("TableName = \"Test\"");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GenerateDdlUsesDefaultSchemaWhenNotSpecified()
    {
        var source = @"

namespace Test
{
    [ManagedConfiguration]
    public partial class TestConfiguration
    {
        public string Name { get; set; } = string.Empty;
    }
}";

        // Act
        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        // Assert: DDL does NOT explicitly set Schema — DdlDefinition.Schema defaults to "cfg"
        var ddl = CompilationHelper.GetGeneratedOutput(compilation, "TestConfiguration.Ddl.g.cs");
        ddl.ShouldNotBeNull();
        ddl.ShouldNotContain("Schema = ");
        ddl.ShouldContain("TableName = \"Test\"");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GenerateDdlExcludesIdAndNameColumnsFromUserProperties()
    {
        // Arrange - has Id and Name properties, but they should be handled specially
        var source = @"

namespace Test
{
    [ManagedConfiguration]
    public partial class TestConfiguration
    {
        public System.Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Server { get; set; } = string.Empty;
    }
}";

        // Act
        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        // Assert
        var ddl = CompilationHelper.GetGeneratedOutput(compilation, "TestConfiguration.Ddl.g.cs");
        ddl.ShouldNotBeNull();

        // Id and Name should appear in the standard columns section, not user columns
        // Server should appear in user columns
        ddl.ShouldContain("Name = \"Server\"");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GenerateDdlExcludesNavigationPropertiesFromColumns()
    {
        // Arrange - ScheduleConfiguration has CronSchedule and IntervalSchedule navigation
        // properties whose types are themselves [ManagedConfiguration] classes. These should
        // NOT become SQL columns because they represent separate child tables.
        var source = @"

namespace Test
{
    [ManagedConfiguration]
    public partial class ScheduleConfiguration
    {
        public string Name { get; set; } = string.Empty;
        public string CronExpression { get; set; } = string.Empty;
        public CronScheduleConfiguration? CronSchedule { get; set; }
        public IntervalScheduleConfiguration? IntervalSchedule { get; set; }
    }

    [ManagedConfiguration]
    public partial class CronScheduleConfiguration
    {
        public string Name { get; set; } = string.Empty;
        public string Expression { get; set; } = string.Empty;
    }

    [ManagedConfiguration]
    public partial class IntervalScheduleConfiguration
    {
        public string Name { get; set; } = string.Empty;
        public int IntervalSeconds { get; set; }
    }
}";

        // Act
        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        // Assert
        var ddl = CompilationHelper.GetGeneratedOutput(compilation, "ScheduleConfiguration.Ddl.g.cs");
        ddl.ShouldNotBeNull();

        // Scalar property must be present
        ddl.ShouldContain("Name = \"CronExpression\"");

        // Navigation properties must NOT appear as columns
        ddl.ShouldNotContain("Name = \"CronSchedule\"");
        ddl.ShouldNotContain("Name = \"IntervalSchedule\"");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GenerateDdlSkipsNameColumnForChildTables()
    {
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
        public int Value { get; set; }
    }
}";

        // Act
        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        // Assert: both parent and child DDL include Name column (all are flat root tables)
        var parentDdl = CompilationHelper.GetGeneratedOutput(compilation, "ParentConfiguration.Ddl.g.cs");
        var childDdl = CompilationHelper.GetGeneratedOutput(compilation, "ChildConfiguration.Ddl.g.cs");

        parentDdl.ShouldNotBeNull();
        childDdl.ShouldNotBeNull();

        parentDdl.ShouldContain("Name = \"Name\"");
        childDdl.ShouldContain("Name = \"Name\"");
    }
    private const string UnsizedSource = @"
namespace Test
{
    [ManagedConfiguration]
    public partial class PriceConfiguration
    {
        public string Name { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
}";

    // FDW-7107: a field that states no size takes the size the build states, so the number in the DDL is the build's.
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void AFieldThatStatesNoSizeTakesTheBuildsSize()
    {
        var sizes = new System.Collections.Generic.Dictionary<string, string>
        {
            [Fdw.Configuration.SourceGenerators.Models.ColumnSizes.StringMaxLengthProperty] = "321",
            [Fdw.Configuration.SourceGenerators.Models.ColumnSizes.DecimalPrecisionProperty] = "12",
            [Fdw.Configuration.SourceGenerators.Models.ColumnSizes.DecimalScaleProperty] = "3",
        };

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(UnsizedSource, sizes);

        diagnostics.ShouldNotContain(d => d.Id == "CFG004");
        var ddl = CompilationHelper.GetGeneratedOutput(compilation, "PriceConfiguration.Ddl.g.cs");
        ddl.ShouldNotBeNull();
        ddl.ShouldContain("MaxLength = 321");
        ddl.ShouldContain("Precision = 12");
        ddl.ShouldContain("Scale = 3");
    }

    // FDW-7107: with no size from the field or the build, each unsized field is CFG004 and the configuration gets no DDL.
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void AFieldWithNoSizeInABuildThatStatesNoneIsReportedAndGetsNoDdl()
    {
        var (compilation, diagnostics) = CompilationHelper.RunGenerator(
            UnsizedSource, new System.Collections.Generic.Dictionary<string, string>());

        var unsized = diagnostics.Where(d => d.Id == "CFG004").Select(d => d.GetMessage(System.Globalization.CultureInfo.InvariantCulture)).ToList();
        unsized.Count.ShouldBe(3);
        unsized.ShouldContain(m => m.Contains("'PriceConfiguration.Label' states no length", System.StringComparison.Ordinal)
            && m.Contains("FDW_DdlStringMaxLength", System.StringComparison.Ordinal));
        unsized.ShouldContain(m => m.Contains("'PriceConfiguration.Amount' states no precision", System.StringComparison.Ordinal));
        unsized.ShouldContain(m => m.Contains("'PriceConfiguration.Amount' states no scale", System.StringComparison.Ordinal));
        CompilationHelper.GetGeneratedOutput(compilation, "PriceConfiguration.Ddl.g.cs").ShouldBeNull();
    }

    // FDW-7107: a field that states its own size needs nothing from the build.
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void AFieldThatStatesItsSizeNeedsNoBuildSize()
    {
        const string source = @"
namespace Test
{
    public sealed class MaxLengthAttribute : System.Attribute
    {
        public MaxLengthAttribute(int length) { }
    }

    [ManagedConfiguration]
    public partial class CodeConfiguration
    {
        public string Name { get; set; } = string.Empty;
        [MaxLength(40)]
        public string Code { get; set; } = string.Empty;
    }
}";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source, new System.Collections.Generic.Dictionary<string, string>());

        diagnostics.ShouldNotContain(d => d.Id == "CFG004");
        CompilationHelper.GetGeneratedOutput(compilation, "CodeConfiguration.Ddl.g.cs")!.ShouldContain("MaxLength = 40");
    }
}
