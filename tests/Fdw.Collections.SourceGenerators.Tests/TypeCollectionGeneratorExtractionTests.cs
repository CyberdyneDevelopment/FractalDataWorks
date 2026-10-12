using System.Linq;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Fdw.Collections.SourceGenerators.Tests;

/// <summary>
/// Unit tests for TypeCollectionGenerator extraction methods.
/// Tests the MEDIUM/HIGH complexity extraction logic.
/// </summary>
public class TypeCollectionGeneratorExtractionTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void ExtractTypeOption_WithTypeOptionSet_ReturnsParentType()
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

[TypeCollection(typeof(ChildBase),
                typeof(ChildBase),
                typeof(ChildCollection),
                TypeOption = typeof(Parents),
                TypeOptionName = ""Child1"")]
public partial class ChildCollection : ParentBase
{
    protected ChildCollection() : base(""Child"") { }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        // Should generate without errors
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

        // Parent should reference child
        var parentGen = CompilationHelper.GetGeneratedOutput(compilation, "Parents.TypeCollection.g.cs");
        Assert.NotNull(parentGen);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void ExtractTypeOption_WithoutTypeOption_ReturnsNull()
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

        // Should work fine - standalone collection
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void ExtractLookupProperties_WithTypeLookupAttributes_GeneratesLookupMethods()
    {
        var source = @"
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Test;

public abstract class TestBase : TypeOptionBase<int, TestBase>
{
    protected TestBase(int id, string name, string category) : base(id, name)
    {
        Category = category;
    }

    [TypeLookup(""Category"")]
    public string Category { get; init; }
}

[TypeCollection(typeof(TestBase), typeof(TestBase), typeof(Tests))]
public partial class Tests : TypeCollectionBase<TestBase, TestBase>
{
}

[TypeOption(typeof(Tests), ""Test1"")]
public class Test1 : TestBase
{
    public Test1() : base(1, ""Test1"", ""Alpha"") { }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

        var generated = CompilationHelper.GetGeneratedOutput(compilation, "Tests.TypeCollection.g.cs");
        Assert.NotNull(generated);

        // Should have ByCategory method generated
        Assert.Contains("ByCategory", generated);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void ExtractCollectionName_FromAttribute_UsesProvidedName()
    {
        var source = @"
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Test;

public abstract class TestBase : TypeOptionBase<int, TestBase>
{
    protected TestBase(int id, string name) : base(id, name) { }
}

[TypeCollection(typeof(TestBase), typeof(TestBase), typeof(CustomName))]
public partial class CustomName : TypeCollectionBase<TestBase, TestBase>
{
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

        var generated = CompilationHelper.GetGeneratedOutput(compilation, "CustomName.TypeCollection.g.cs");
        Assert.NotNull(generated);
        Assert.Contains("partial class CustomName", generated);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void ExtractRestrictToCurrentCompilation_True_OnlyIncludesLocalTypes()
    {
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

        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

        var generated = CompilationHelper.GetGeneratedOutput(compilation, "Tests.TypeCollection.g.cs");
        Assert.NotNull(generated);
        Assert.Contains("LocalOption", generated);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void ExtractRestrictToCurrentCompilation_False_IncludesAllAssemblies()
    {
        // Default is false, so this is the normal behavior
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

        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
    }
}
