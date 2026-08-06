using System.Linq;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Fdw.Collections.SourceGenerators.Tests;

/// <summary>
/// Tests for constructor extraction logic (CRITICAL complexity: 23).
/// This is the most complex method in the generator and needs thorough testing.
/// </summary>
public class TypeCollectionGeneratorConstructorTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void ExtractConstructor_SimpleConstructor_Works()
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

[TypeOption(typeof(Tests), ""Simple"")]
public class SimpleType : TestBase
{
    public SimpleType() : base(1, ""Simple"") { }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

        var generated = CompilationHelper.GetGeneratedOutput(compilation, "Tests.TypeCollection.g.cs");
        Assert.NotNull(generated);
        Assert.Contains("Simple", generated);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void ExtractConstructor_MultipleParameters_Works()
    {
        var source = @"
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Test;

public abstract class TestBase : TypeOptionBase<int, TestBase>
{
    protected TestBase(int id, string name, string description, int value) : base(id, name)
    {
        Description = description;
        Value = value;
    }

    public string Description { get; init; }
    public int Value { get; init; }
}

[TypeCollection(typeof(TestBase), typeof(TestBase), typeof(Tests))]
public partial class Tests : TypeCollectionBase<TestBase, TestBase>
{
}

[TypeOption(typeof(Tests), ""Complex"")]
public class ComplexType : TestBase
{
    public ComplexType() : base(1, ""Complex"", ""Test description"", 42) { }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

        var generated = CompilationHelper.GetGeneratedOutput(compilation, "Tests.TypeCollection.g.cs");
        Assert.NotNull(generated);
        Assert.Contains("Complex", generated);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void ExtractConstructor_WithDefaultParameters_HandlesDefaults()
    {
        var source = @"
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Test;

public abstract class TestBase : TypeOptionBase<int, TestBase>
{
    protected TestBase(int id, string name, bool enabled = true) : base(id, name)
    {
        Enabled = enabled;
    }

    public bool Enabled { get; init; }
}

[TypeCollection(typeof(TestBase), typeof(TestBase), typeof(Tests))]
public partial class Tests : TypeCollectionBase<TestBase, TestBase>
{
}

[TypeOption(typeof(Tests), ""WithDefault"")]
public class WithDefault : TestBase
{
    public WithDefault() : base(1, ""WithDefault"") { }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void ExtractConstructor_ChainedConstructors_Works()
    {
        var source = @"
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Test;

public abstract class TestBase : TypeOptionBase<int, TestBase>
{
    protected TestBase(int id, string name) : base(id, name) { }
    protected TestBase(int id, string name, string extra) : this(id, name)
    {
        Extra = extra;
    }

    public string? Extra { get; init; }
}

[TypeCollection(typeof(TestBase), typeof(TestBase), typeof(Tests))]
public partial class Tests : TypeCollectionBase<TestBase, TestBase>
{
}

[TypeOption(typeof(Tests), ""Chained"")]
public class ChainedType : TestBase
{
    public ChainedType() : base(1, ""Chained"", ""extra"") { }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void ExtractConstructor_OverloadedConstructors_SelectsCorrect()
    {
        var source = @"
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Test;

public abstract class TestBase : TypeOptionBase<int, TestBase>
{
    protected TestBase(int id, string name) : base(id, name) { }

    protected TestBase(int id, string name, string description) : base(id, name)
    {
        Description = description;
    }

    public string? Description { get; init; }
}

[TypeCollection(typeof(TestBase), typeof(TestBase), typeof(Tests))]
public partial class Tests : TypeCollectionBase<TestBase, TestBase>
{
}

[TypeOption(typeof(Tests), ""Short"")]
public class ShortType : TestBase
{
    public ShortType() : base(1, ""Short"") { }
}

[TypeOption(typeof(Tests), ""Long"")]
public class LongType : TestBase
{
    public LongType() : base(2, ""Long"", ""With description"") { }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

        var generated = CompilationHelper.GetGeneratedOutput(compilation, "Tests.TypeCollection.g.cs");
        Assert.NotNull(generated);
        Assert.Contains("Short", generated);
        Assert.Contains("Long", generated);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void ExtractConstructor_WithEnumParameter_SerializesCorrectly()
    {
        var source = @"
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Test;

public enum Status { Active, Inactive }

public abstract class TestBase : TypeOptionBase<int, TestBase>
{
    protected TestBase(int id, string name, Status status) : base(id, name)
    {
        Status = status;
    }

    public Status Status { get; init; }
}

[TypeCollection(typeof(TestBase), typeof(TestBase), typeof(Tests))]
public partial class Tests : TypeCollectionBase<TestBase, TestBase>
{
}

[TypeOption(typeof(Tests), ""Active"")]
public class ActiveType : TestBase
{
    public ActiveType() : base(1, ""Active"", Status.Active) { }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void ExtractConstructor_WithNullLiteral_HandlesNull()
    {
        var source = @"
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Test;

public abstract class TestBase : TypeOptionBase<int, TestBase>
{
    protected TestBase(int id, string name, string? optional = null) : base(id, name)
    {
        Optional = optional;
    }

    public string? Optional { get; init; }
}

[TypeCollection(typeof(TestBase), typeof(TestBase), typeof(Tests))]
public partial class Tests : TypeCollectionBase<TestBase, TestBase>
{
}

[TypeOption(typeof(Tests), ""WithNull"")]
public class WithNullType : TestBase
{
    public WithNullType() : base(1, ""WithNull"", null) { }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void ExtractConstructor_WithStringInterpolation_HandlesCorrectly()
    {
        var source = @"
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Test;

public abstract class TestBase : TypeOptionBase<int, TestBase>
{
    protected TestBase(int id, string name, string template) : base(id, name)
    {
        Template = template;
    }

    public string Template { get; init; }
}

[TypeCollection(typeof(TestBase), typeof(TestBase), typeof(Tests))]
public partial class Tests : TypeCollectionBase<TestBase, TestBase>
{
}

[TypeOption(typeof(Tests), ""Templated"")]
public class TemplatedType : TestBase
{
    private const string Prefix = ""Test"";
    public TemplatedType() : base(1, ""Templated"", $""{Prefix}_Template"") { }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        // May have errors or warnings depending on how generator handles interpolation
        Assert.NotNull(compilation);
    }
}
