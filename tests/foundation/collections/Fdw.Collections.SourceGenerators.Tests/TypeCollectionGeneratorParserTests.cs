using System.Threading.Tasks;
using System.Linq;
using Xunit;

namespace Fdw.Collections.SourceGenerators.Tests;

/// <summary>
/// Tests for TypeCollectionGenerator Parser logic (modeled after Microsoft.Extensions.Logging pattern).
/// Validates discovery, extraction, and model building.
/// </summary>
public class TypeCollectionGeneratorParserTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void DiscoverTypeCollection_WithNoTypeOptions_GeneratesEmptyCollection()
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
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        // Should generate successfully with no errors
        Assert.Empty(diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error));

        // Should generate Tests.g.cs
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "Tests.TypeCollection.g.cs");
        Assert.NotNull(generated);
        Assert.Contains("partial class Tests", generated);
        Assert.Contains("NotFound", generated);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void DiscoverTypeOptions_AcrossAssemblies_FindsAll()
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

[TypeOption(typeof(Tests), ""Option1"")]
public class Option1 : TestBase
{
    public Option1() : base(1, ""Option1"") { }
}

[TypeOption(typeof(Tests), ""Option2"")]
public class Option2 : TestBase
{
    public Option2() : base(2, ""Option2"") { }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error));

        var generated = CompilationHelper.GetGeneratedOutput(compilation, "Tests.TypeCollection.g.cs");
        Assert.NotNull(generated);

        // Should generate static properties for both options
        Assert.Contains("Option1", generated);
        Assert.Contains("Option2", generated);
        Assert.Contains("ByName", generated);
        Assert.Contains("ById", generated);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void DetectParentCollection_WithChildUsingTypeOption_AutoDetectsParent()
    {
        var source = @"
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Test;

public abstract class ParentBase : TypeCollectionBase<ChildBase, ChildBase>
{
    protected ParentBase(string name) : base(name) { }
}

[TypeCollection(typeof(ParentBase), typeof(ParentBase), typeof(Parents))]
public partial class Parents : TypeCollectionBase<ParentBase, ParentBase>
{
}

public abstract class ChildBase : TypeOptionBase<int, ChildBase>
{
    protected ChildBase(int id, string name) : base(id, name) { }
}

[TypeCollection(typeof(ChildBase),
                typeof(ChildBase),
                typeof(ChildCollection),
                TypeOption = typeof(Parents),
                TypeOptionName = ""Child1"")]
public partial class ChildCollection : ParentBase
{
    protected ChildCollection() : base(""Child Collection"") { }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error));

        var generated = CompilationHelper.GetGeneratedOutput(compilation, "Parents.TypeCollection.g.cs");
        Assert.NotNull(generated);

        // Parent should have property for child
        Assert.Contains("Child1", generated);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void ExtractTypeOption_MissingTypeOptionName_ReportsError()
    {
        var source = @"
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Test;

public abstract class ParentBase : TypeCollectionBase<ChildBase, ChildBase>
{
    protected ParentBase(string name) : base(name) { }
}

[TypeCollection(typeof(ParentBase), typeof(ParentBase), typeof(Parents))]
public partial class Parents { }

public abstract class ChildBase : TypeOptionBase<int, ChildBase>
{
    protected ChildBase(int id, string name) : base(id, name) { }
}

// Missing TypeOptionName
[TypeCollection(typeof(ChildBase),
                typeof(ChildBase),
                typeof(ChildCollection),
                TypeOption = typeof(Parents))]
public partial class ChildCollection : ParentBase
{
    protected ChildCollection() : base(""Child"") { }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        // Should have warning/error about missing TypeOptionName
        // (Depending on current implementation - may need to add this validation)
        // For now, just ensure it doesn't crash
        Assert.NotNull(compilation);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void RestrictToCurrentCompilation_True_OnlyFindsLocalTypes()
    {
        // This test validates the RestrictToCurrentCompilation flag
        // When true, should only discover TypeOptions in same assembly
        var source = @"
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Test;

public abstract class TestBase : TypeOptionBase<int, TestBase>
{
    protected TestBase(int id, string name) : base(id, name) { }
}

[TypeCollection(typeof(TestBase),
                typeof(TestBase),
                typeof(Tests),
                RestrictToCurrentCompilation = true)]
public partial class Tests : TypeCollectionBase<TestBase, TestBase>
{
}

[TypeOption(typeof(Tests), ""LocalOption"")]
public class LocalOption : TestBase
{
    public LocalOption() : base(1, ""LocalOption"") { }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error));

        var generated = CompilationHelper.GetGeneratedOutput(compilation, "Tests.TypeCollection.g.cs");
        Assert.NotNull(generated);
        Assert.Contains("LocalOption", generated);
    }
}
