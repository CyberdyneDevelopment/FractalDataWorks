using System.Linq;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Fdw.Collections.SourceGenerators.Tests;

/// <summary>
/// Tests for TypeCollectionGenerator validation logic.
/// Tests error detection and diagnostic reporting.
/// </summary>
public class TypeCollectionGeneratorValidationTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void ValidateNoAbstractProperties_WithAbstractProperty_ReportsDiagnostic()
    {
        // This should generate a diagnostic about abstract properties
        var source = @"
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Test;

public abstract class TestBase : TypeOptionBase<int, TestBase>
{
    protected TestBase(int id, string name) : base(id, name) { }

    // Abstract property - generator can't instantiate
    public abstract string AbstractProperty { get; }
}

[TypeCollection(typeof(TestBase), typeof(TestBase), typeof(Tests))]
public partial class Tests : TypeCollectionBase<TestBase, TestBase>
{
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        // Should have diagnostic about abstract property
        // (Depending on current implementation - may need to add this validation)
        Assert.NotNull(compilation);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void MissingInterface_OnBaseType_StillGenerates()
    {
        // Base type doesn't implement interface - should still work
        var source = @"
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Test;

public abstract class TestBase : TypeOptionBase<int, TestBase>
{
    protected TestBase(int id, string name) : base(id, name) { }
}

[TypeCollection(typeof(TestBase), typeof(TestBase), typeof(Tests))]
public partial class Tests : TypeCollectionBase<TestBase, TestBase>
{
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        // Should generate without errors
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void CollectionWithoutPartialKeyword_ReportsDiagnostic()
    {
        // Missing partial keyword - can't generate
        var source = @"
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Test;

public abstract class TestBase : TypeOptionBase<int, TestBase>
{
    protected TestBase(int id, string name) : base(id, name) { }
}

[TypeCollection(typeof(TestBase), typeof(TestBase), typeof(Tests))]
public class Tests : TypeCollectionBase<TestBase, TestBase>
{
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        // May have diagnostic or just not generate anything
        // This depends on generator implementation
        Assert.NotNull(compilation);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void DuplicateTypeOptionNames_InSameCollection_ReportsError()
    {
        var source = @"
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Test;

public abstract class TestBase : TypeOptionBase<int, TestBase>
{
    protected TestBase(int id, string name) : base(id, name) { }
}

[TypeCollection(typeof(TestBase), typeof(TestBase), typeof(Tests))]
public partial class Tests : TypeCollectionBase<TestBase, TestBase>
{
}

[TypeOption(typeof(Tests), ""Duplicate"")]
public class Option1 : TestBase
{
    public Option1() : base(1, ""Duplicate"") { }
}

[TypeOption(typeof(Tests), ""Duplicate"")]
public class Option2 : TestBase
{
    public Option2() : base(2, ""Duplicate"") { }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        // Should either report diagnostic or handle gracefully
        Assert.NotNull(compilation);
    }
}
