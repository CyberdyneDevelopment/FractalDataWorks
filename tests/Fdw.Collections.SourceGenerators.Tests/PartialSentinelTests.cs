using System.Linq;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Fdw.Collections.SourceGenerators.Tests;

/// <summary>
/// Tests for the partial NotFound sentinel feature.
/// Verifies that user-declared partial NotFound classes can override individual members
/// while the generator fills in the rest.
/// </summary>
public class PartialSentinelTests
{
    #region Backward Compatibility

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void TypeCollection_NoUserPartial_GeneratesAllStubs()
    {
        // Arrange -- standard TypeCollection with abstract members, no user-declared partial
        var source = @"
using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Test;

public abstract class AnimalBase : TypeOptionBase<int, AnimalBase>
{
    protected AnimalBase(int id, string name) : base(id, name) { }

    public abstract IReadOnlyList<string> Sounds { get; }
    public abstract bool CanFly { get; }
}

[TypeCollection(typeof(AnimalBase), typeof(AnimalBase), typeof(Animals))]
public partial class Animals : TypeCollectionBase<AnimalBase, AnimalBase>
{
}

[TypeOption(typeof(Animals), ""Dog"")]
public class Dog : AnimalBase
{
    public Dog() : base(1, ""Dog"") { }
    public override IReadOnlyList<string> Sounds => new[] { ""Bark"" };
    public override bool CanFly => false;
}
";

        // Act
        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        // Assert -- no errors, NotFound sentinel generated with all abstract stubs
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

        var generated = CompilationHelper.GetGeneratedOutput(compilation, "Animals.TypeCollection.g.cs");
        Assert.NotNull(generated);
        Assert.Contains("partial class NotFoundAnimals", generated);
        Assert.Contains("Sounds", generated);
        Assert.Contains("CanFly", generated);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void TypeCollection_ExistingCollections_CompileUnchanged()
    {
        // Arrange -- simple collection with no abstract members beyond base
        var source = @"
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Test;

public abstract class StatusBase : TypeOptionBase<int, StatusBase>
{
    protected StatusBase(int id, string name) : base(id, name) { }
}

[TypeCollection(typeof(StatusBase), typeof(StatusBase), typeof(Statuses))]
public partial class Statuses : TypeCollectionBase<StatusBase, StatusBase>
{
}

[TypeOption(typeof(Statuses), ""Active"")]
public class ActiveStatus : StatusBase
{
    public ActiveStatus() : base(1, ""Active"") { }
}

[TypeOption(typeof(Statuses), ""Inactive"")]
public class InactiveStatus : StatusBase
{
    public InactiveStatus() : base(2, ""Inactive"") { }
}
";

        // Act
        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        // Assert -- compiles with no errors, backward compatible
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

        var generated = CompilationHelper.GetGeneratedOutput(compilation, "Statuses.TypeCollection.g.cs");
        Assert.NotNull(generated);
        Assert.Contains("partial class Statuses", generated);
        Assert.Contains("Active", generated);
        Assert.Contains("Inactive", generated);
        Assert.Contains("NotFound", generated);
    }

    #endregion

    #region TypeCollection Partial NotFound

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void TypeCollection_UserPartialOverridesOneMember_GeneratorSkipsThatMember()
    {
        // Arrange -- user declares partial NotFound class that overrides one abstract member
        var source = @"
using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Test;

public abstract class HandlerBase : TypeOptionBase<int, HandlerBase>
{
    protected HandlerBase(int id, string name) : base(id, name) { }

    public abstract IReadOnlyList<int> ErrorNumbers { get; }
    public abstract bool IsRetryable { get; }
    public abstract string CreateMessage(string context);
}

[TypeCollection(typeof(HandlerBase), typeof(HandlerBase), typeof(Handlers))]
public partial class Handlers : TypeCollectionBase<HandlerBase, HandlerBase>
{
    private partial class NotFoundHandlers
    {
        public override string CreateMessage(string context) => ""Unknown error"";
    }
}

[TypeOption(typeof(Handlers), ""Timeout"")]
public class TimeoutHandler : HandlerBase
{
    public TimeoutHandler() : base(1, ""Timeout"") { }
    public override IReadOnlyList<int> ErrorNumbers => new[] { -2 };
    public override bool IsRetryable => true;
    public override string CreateMessage(string context) => ""Timed out"";
}
";

        // Act
        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        // Assert
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

        var generated = CompilationHelper.GetGeneratedOutput(compilation, "Handlers.TypeCollection.g.cs");
        Assert.NotNull(generated);

        // Generator should still emit stubs for ErrorNumbers and IsRetryable
        Assert.Contains("ErrorNumbers", generated);
        Assert.Contains("IsRetryable", generated);

        // Generator should NOT emit a stub for CreateMessage (user declared it)
        var sentinelSection = GetSentinelSection(generated, "NotFoundHandlers");
        Assert.NotNull(sentinelSection);
        Assert.DoesNotContain("CreateMessage", sentinelSection);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void TypeCollection_UserPartialOverridesAllMembers_GeneratorEmitsNoStubs()
    {
        // Arrange -- user declares partial that overrides ALL abstract members
        var source = @"
using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Test;

public abstract class ItemBase : TypeOptionBase<int, ItemBase>
{
    protected ItemBase(int id, string name) : base(id, name) { }

    public abstract string Label { get; }
    public abstract int Priority { get; }
}

[TypeCollection(typeof(ItemBase), typeof(ItemBase), typeof(Items))]
public partial class Items : TypeCollectionBase<ItemBase, ItemBase>
{
    private partial class NotFoundItems
    {
        public override string Label => ""Not Found"";
        public override int Priority => -1;
    }
}

[TypeOption(typeof(Items), ""High"")]
public class HighItem : ItemBase
{
    public HighItem() : base(1, ""High"") { }
    public override string Label => ""High Priority"";
    public override int Priority => 1;
}
";

        // Act
        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        // Assert
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

        var generated = CompilationHelper.GetGeneratedOutput(compilation, "Items.TypeCollection.g.cs");
        Assert.NotNull(generated);

        // The sentinel class should still be generated (partial)
        Assert.Contains("partial class NotFoundItems", generated);

        // But the generated code should NOT contain stubs for Label or Priority
        // since the user already declared them
        var sentinelSection = GetSentinelSection(generated, "NotFoundItems");
        Assert.NotNull(sentinelSection);
        Assert.DoesNotContain("Label", sentinelSection);
        Assert.DoesNotContain("Priority", sentinelSection);
    }

    #endregion

    #region Generated Sentinel Is Partial

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void TypeCollection_GeneratedSentinel_IsPartialClass()
    {
        // Arrange -- any TypeCollection
        var source = @"
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Test;

public abstract class SimpleBase : TypeOptionBase<int, SimpleBase>
{
    protected SimpleBase(int id, string name) : base(id, name) { }
}

[TypeCollection(typeof(SimpleBase), typeof(SimpleBase), typeof(Simples))]
public partial class Simples : TypeCollectionBase<SimpleBase, SimpleBase>
{
}

[TypeOption(typeof(Simples), ""One"")]
public class OneSimple : SimpleBase
{
    public OneSimple() : base(1, ""One"") { }
}
";

        // Act
        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        // Assert -- sentinel must be 'partial class', not 'sealed class'
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

        var generated = CompilationHelper.GetGeneratedOutput(compilation, "Simples.TypeCollection.g.cs");
        Assert.NotNull(generated);
        Assert.Contains("private partial class NotFoundSimples", generated);
        Assert.DoesNotContain("private sealed class NotFoundSimples", generated);
    }

    #endregion

    #region Helpers

    private static string? GetSentinelSection(string generatedCode, string sentinelClassName)
    {
        var startMarker = $"class {sentinelClassName}";
        var startIndex = generatedCode.IndexOf(startMarker, System.StringComparison.Ordinal);
        if (startIndex < 0) return null;

        // Find the matching closing brace by counting braces
        var braceCount = 0;
        var inSection = false;
        for (var i = startIndex; i < generatedCode.Length; i++)
        {
            if (generatedCode[i] == '{')
            {
                braceCount++;
                inSection = true;
            }
            else if (generatedCode[i] == '}')
            {
                braceCount--;
                if (inSection && braceCount == 0)
                {
                    return generatedCode.Substring(startIndex, i - startIndex + 1);
                }
            }
        }

        return null;
    }

    #endregion
}
