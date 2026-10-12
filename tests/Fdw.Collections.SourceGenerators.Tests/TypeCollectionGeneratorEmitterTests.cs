using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Fdw.Collections.SourceGenerators.Tests;

/// <summary>
/// Tests for TypeCollectionGenerator Emitter logic (modeled after Microsoft pattern).
/// Validates code generation output structure and content.
/// </summary>
public class TypeCollectionGeneratorEmitterTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void GeneratedCode_HasCorrectNamespace()
    {
        var source = @"
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace MyNamespace.Collections;

public abstract class TestBase : TypeOptionBase<int, TestBase>
{
    protected TestBase(int id, string name) : base(id, name) { }
}

[TypeCollection(typeof(TestBase), typeof(TestBase), typeof(Tests))]
public partial class Tests : TypeCollectionBase<TestBase, TestBase>
{
}
";

        var (compilation, _) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "Tests.TypeCollection.g.cs");

        Assert.NotNull(generated);
        Assert.Contains("namespace MyNamespace.Collections", generated);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void GeneratedCode_IncludesFrozenDictionary()
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
";

        var (compilation, _) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "Tests.TypeCollection.g.cs");

        Assert.NotNull(generated);

        // TypeCollection uses FrozenDictionary for immutable collections
        Assert.Contains("FrozenDictionary", generated);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void GeneratedCode_HasStaticConstructor()
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

        var (compilation, _) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "Tests.TypeCollection.g.cs");

        Assert.NotNull(generated);
        Assert.Contains("static Tests()", generated);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void GeneratedCode_ForMutableCollection_UsesConcurrentDictionary()
    {
        var source = @"
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Test;

public abstract class TestBase : TypeOptionBase<int, TestBase>
{
    protected TestBase(int id, string name) : base(id, name) { }
}

[MutableTypeCollection(typeof(TestBase), typeof(TestBase), typeof(Tests))]
public partial class Tests : TypeCollectionBase<TestBase, TestBase>
{
}
";

        var (compilation, _) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "Tests.g.cs");

        Assert.NotNull(generated);

        // MutableTypeCollection should use ConcurrentDictionary
        Assert.Contains("ConcurrentDictionary", generated);

        // Should have Register method
        Assert.Contains("Register", generated);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void GeneratedCode_ForFactoryCollection_HasCreateMethods()
    {
        var source = @"
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Test;

public abstract class TestBase : TypeOptionBase<int, TestBase>
{
    protected TestBase(int id, string name) : base(id, name) { }
}

[TypeInstanceCollection(typeof(TestBase), typeof(TestBase), typeof(TestFactories))]
public partial class TestFactories : TypeCollectionBase<TestBase, TestBase>
{
}

[TypeOption(typeof(TestFactories), ""Option1"")]
public class Option1 : TestBase
{
    public Option1() : base(1, ""Option1"") { }
}
";

        var (compilation, _) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "TestFactories.g.cs");

        Assert.NotNull(generated);

        // Factory collections should have Create methods
        Assert.Contains("Create", generated);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void GeneratedCode_HasEmptyMethod()
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

        var (compilation, _) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "Tests.TypeCollection.g.cs");

        Assert.NotNull(generated);
        Assert.Contains("NotFound", generated);
        Assert.Contains("NotFoundTests", generated);
    }
}
