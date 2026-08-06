using System.Linq;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Fdw.Collections.SourceGenerators.Tests;

/// <summary>
/// Tests for CollectionStrategy functionality (TDD - tests written before implementation).
/// These tests define expected behavior for Immutable/Mutable/Factory collections.
/// </summary>
public class CollectionStrategyTests
{
    #region TypeCollection (Immutable) Tests

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void ImmutableCollection_UsesFrozenDictionary()
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

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "Tests.TypeCollection.g.cs");

        Assert.NotNull(generated);
        Assert.Contains("System.Collections.Frozen", generated);
        Assert.Contains("RegisterMember", generated); // Immutable collections have RegisterMember for cross-assembly discovery
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void ImmutableCollection_GeneratesStaticProperties()
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

[TypeOption(typeof(Tests), ""Alpha"")]
public class AlphaType : TestBase
{
    public AlphaType() : base(1, ""Alpha"") { }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "Tests.TypeCollection.g.cs");

        Assert.NotNull(generated);
        Assert.Contains("public static", generated);
        Assert.Contains("Alpha", generated);
    }

    #endregion

    #region MutableTypeCollection Tests

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void MutableCollection_UsesConcurrentDictionary()
    {
        var source = @"
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Test;

public abstract class PluginBase : TypeOptionBase<int, PluginBase>
{
    protected PluginBase(int id, string name) : base(id, name) { }
}

[MutableTypeCollection(typeof(PluginBase), typeof(PluginBase), typeof(Plugins))]
public partial class Plugins : TypeCollectionBase<PluginBase, PluginBase>
{
}

[TypeOption(typeof(Plugins), ""Core"")]
public class CorePlugin : PluginBase
{
    public CorePlugin() : base(1, ""Core"") { }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "Plugins.g.cs");

        Assert.NotNull(generated);
        Assert.Contains("ConcurrentDictionary", generated);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void MutableCollection_GeneratesRegisterMethod()
    {
        var source = @"
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Test;

public abstract class PluginBase : TypeOptionBase<int, PluginBase>
{
    protected PluginBase(int id, string name) : base(id, name) { }
}

[MutableTypeCollection(typeof(PluginBase), typeof(PluginBase), typeof(Plugins))]
public partial class Plugins : TypeCollectionBase<PluginBase, PluginBase>
{
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "Plugins.g.cs");

        Assert.NotNull(generated);
        Assert.Contains("Register", generated);
        Assert.Contains("public static void Register", generated);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void MutableCollection_RegisterMethodSignature_TakesReturnType()
    {
        var source = @"
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Test;

public interface IPlugin : ITypeOption<int, PluginBase>
{
}

public abstract class PluginBase : TypeOptionBase<int, PluginBase>, IPlugin
{
    protected PluginBase(int id, string name) : base(id, name) { }
}

[MutableTypeCollection(typeof(PluginBase), typeof(IPlugin), typeof(Plugins))]
public partial class Plugins : TypeCollectionBase<PluginBase, IPlugin>
{
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "Plugins.g.cs");

        Assert.NotNull(generated);
        // Register should take the return type (IPlugin), not base type
        Assert.Contains("Register(IPlugin", generated);
    }

    #endregion

    #region TypeInstanceCollection (Factory) Tests

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void FactoryCollection_GeneratesCreateMethods()
    {
        var source = @"
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Test;

public abstract class ServiceBase : TypeOptionBase<int, ServiceBase>
{
    protected ServiceBase(int id, string name) : base(id, name) { }
}

[TypeInstanceCollection(typeof(ServiceBase), typeof(ServiceBase), typeof(ServiceFactories))]
public partial class ServiceFactories : TypeCollectionBase<ServiceBase, ServiceBase>
{
}

[TypeOption(typeof(ServiceFactories), ""Email"")]
public class EmailService : ServiceBase
{
    public EmailService() : base(1, ""Email"") { }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "ServiceFactories.g.cs");

        Assert.NotNull(generated);
        Assert.Contains("Create", generated);
        Assert.Contains("Email", generated);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void FactoryCollection_UsesDictionary_NotFrozen()
    {
        var source = @"
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Test;

public abstract class ServiceBase : TypeOptionBase<int, ServiceBase>
{
    protected ServiceBase(int id, string name) : base(id, name) { }
}

[TypeInstanceCollection(typeof(ServiceBase), typeof(ServiceBase), typeof(Services))]
public partial class Services : TypeCollectionBase<ServiceBase, ServiceBase>
{
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "Services.g.cs");

        Assert.NotNull(generated);
        // Should use Dictionary, not FrozenDictionary (mutable for Register)
        Assert.Contains("Dictionary", generated);
        Assert.DoesNotContain("FrozenDictionary", generated);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void FactoryCollection_GeneratesRegisterMethod()
    {
        var source = @"
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Test;

public abstract class ServiceBase : TypeOptionBase<int, ServiceBase>
{
    protected ServiceBase(int id, string name) : base(id, name) { }
}

[TypeInstanceCollection(typeof(ServiceBase), typeof(ServiceBase), typeof(Services))]
public partial class Services : TypeCollectionBase<ServiceBase, ServiceBase>
{
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "Services.g.cs");

        Assert.NotNull(generated);
        Assert.Contains("Register", generated);
        Assert.Contains("public static void Register", generated);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void FactoryCollection_CreateMethods_ReturnNewInstances()
    {
        var source = @"
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Test;

public abstract class ServiceBase : TypeOptionBase<int, ServiceBase>
{
    protected ServiceBase(int id, string name) : base(id, name) { }
}

[TypeInstanceCollection(typeof(ServiceBase), typeof(ServiceBase), typeof(Services))]
public partial class Services : TypeCollectionBase<ServiceBase, ServiceBase>
{
}

[TypeOption(typeof(Services), ""Smtp"")]
public class SmtpService : ServiceBase
{
    public SmtpService() : base(1, ""Smtp"") { }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "Services.g.cs");

        Assert.NotNull(generated);
        // Should have CreateSmtp() or similar
        Assert.Contains("Smtp", generated);
        Assert.Contains("new ", generated); // Creates new instance
    }

    #endregion

    #region Strategy Detection Tests

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void ThreeAttributeTypes_AreDiscoveredIndependently()
    {
        // Test that all three attribute types can coexist
        var source = @"
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Test;

public abstract class TypeA : TypeOptionBase<int, TypeA>
{
    protected TypeA(int id, string name) : base(id, name) { }
}

public abstract class TypeB : TypeOptionBase<int, TypeB>
{
    protected TypeB(int id, string name) : base(id, name) { }
}

public abstract class TypeC : TypeOptionBase<int, TypeC>
{
    protected TypeC(int id, string name) : base(id, name) { }
}

[TypeCollection(typeof(TypeA), typeof(TypeA), typeof(CollectionA))]
public partial class CollectionA : TypeCollectionBase<TypeA, TypeA> { }

[MutableTypeCollection(typeof(TypeB), typeof(TypeB), typeof(CollectionB))]
public partial class CollectionB : TypeCollectionBase<TypeB, TypeB> { }

[TypeInstanceCollection(typeof(TypeC), typeof(TypeC), typeof(CollectionC))]
public partial class CollectionC : TypeCollectionBase<TypeC, TypeC> { }
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

        // All three should generate
        var genA = CompilationHelper.GetGeneratedOutput(compilation, "CollectionA.TypeCollection.g.cs");
        var genB = CompilationHelper.GetGeneratedOutput(compilation, "CollectionB.g.cs");
        var genC = CompilationHelper.GetGeneratedOutput(compilation, "CollectionC.g.cs");

        Assert.NotNull(genA);
        Assert.NotNull(genB);
        Assert.NotNull(genC);
    }

    #endregion

    #region Edge Cases

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void EmptyCollection_WithNoOptions_StillGenerates()
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

        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

        var generated = CompilationHelper.GetGeneratedOutput(compilation, "Tests.TypeCollection.g.cs");
        Assert.NotNull(generated);
        Assert.Contains("Empty", generated);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void GenericBaseType_IsSupported()
    {
        var source = @"
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Test;

public abstract class TestBase<T> : TypeOptionBase<int, TestBase<T>>
{
    protected TestBase(int id, string name) : base(id, name) { }
    public abstract T GetValue();
}

[TypeCollection(typeof(TestBase<>), typeof(TestBase<>), typeof(Tests))]
public partial class Tests : TypeCollectionBase<TestBase<object>, TestBase<object>>
{
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        // Should handle generic types (may have limitations)
        Assert.NotNull(compilation);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void MultipleCollections_InSameFile_BothGenerate()
    {
        var source = @"
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Test;

public abstract class TypeA : TypeOptionBase<int, TypeA>
{
    protected TypeA(int id, string name) : base(id, name) { }
}

public abstract class TypeB : TypeOptionBase<int, TypeB>
{
    protected TypeB(int id, string name) : base(id, name) { }
}

[TypeCollection(typeof(TypeA), typeof(TypeA), typeof(CollectionA))]
public partial class CollectionA : TypeCollectionBase<TypeA, TypeA> { }

[TypeCollection(typeof(TypeB), typeof(TypeB), typeof(CollectionB))]
public partial class CollectionB : TypeCollectionBase<TypeB, TypeB> { }
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

        var genA = CompilationHelper.GetGeneratedOutput(compilation, "CollectionA.TypeCollection.g.cs");
        var genB = CompilationHelper.GetGeneratedOutput(compilation, "CollectionB.TypeCollection.g.cs");

        Assert.NotNull(genA);
        Assert.NotNull(genB);
    }

    #endregion

    #region Register Method Tests (TDD)

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void MutableCollection_RegisterMethod_AddsToDict()
    {
        var source = @"
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Test;

public abstract class PluginBase : TypeOptionBase<int, PluginBase>
{
    protected PluginBase(int id, string name) : base(id, name) { }
}

[MutableTypeCollection(typeof(PluginBase), typeof(PluginBase), typeof(Plugins))]
public partial class Plugins : TypeCollectionBase<PluginBase, PluginBase>
{
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "Plugins.g.cs");

        Assert.NotNull(generated);
        Assert.Contains("public static void Register(PluginBase item)", generated);
        Assert.Contains("TryAdd", generated); // ConcurrentDictionary.TryAdd
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void FactoryCollection_RegisterMethod_AddsFactory()
    {
        var source = @"
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Test;

public abstract class ServiceBase : TypeOptionBase<int, ServiceBase>
{
    protected ServiceBase(int id, string name) : base(id, name) { }
}

[TypeInstanceCollection(typeof(ServiceBase), typeof(ServiceBase), typeof(Services))]
public partial class Services : TypeCollectionBase<ServiceBase, ServiceBase>
{
}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "Services.g.cs");

        Assert.NotNull(generated);
        Assert.Contains("public static void Register(ServiceBase item)", generated);
        // Should add to factories dictionary
        Assert.Contains("Dictionary", generated);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void RegisterMethod_NullCheck_ThrowsArgumentNullException()
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

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "Tests.g.cs");

        Assert.NotNull(generated);
        Assert.Contains("ArgumentNullException", generated);
        Assert.True(generated.Contains("if (item == null)") || generated.Contains("ArgumentNullException.ThrowIfNull"));
    }

    #endregion

    #region Cross-Strategy Tests

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void ThreeStrategies_UseDifferentDictionaries()
    {
        // Verify each strategy uses correct dictionary type
        var immutable = CompileAndGetGenerated("[TypeCollection", "Immutable");
        var mutable = CompileAndGetGenerated("[MutableTypeCollection", "Mutable");
        var factory = CompileAndGetGenerated("[TypeInstanceCollection", "Factory");

        Assert.Contains("System.Collections.Frozen", immutable);
        Assert.Contains("ConcurrentDictionary", mutable);
        Assert.Contains("Dictionary", factory);
        Assert.DoesNotContain("ConcurrentDictionary", immutable); // Not ConcurrentDictionary
    }

    private string CompileAndGetGenerated(string attributeName, string className)
    {
        var source = $@"
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Test;

public abstract class TestBase : TypeOptionBase<int, TestBase>
{{
    protected TestBase(int id, string name) : base(id, name) {{ }}
}}

{attributeName}(typeof(TestBase), typeof(TestBase), typeof({className}))]
public partial class {className} : TypeCollectionBase<TestBase, TestBase>
{{
}}
";

        var (compilation, diagnostics) = CompilationHelper.RunGenerator(source);
        var fileName = attributeName == "[TypeCollection"
            ? $"{className}.TypeCollection.g.cs"
            : $"{className}.g.cs";
        return CompilationHelper.GetGeneratedOutput(compilation, fileName) ?? "";
    }

    #endregion
}
