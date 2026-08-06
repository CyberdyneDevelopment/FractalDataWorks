using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Shouldly;
using Xunit;

namespace Fdw.Registration.SourceGenerators.Tests;

public class TypeOptionModuleInitializerGeneratorTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void GeneratorSkipsAssemblyDefiningTypeOptions()
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
public abstract partial class Tests : TypeCollectionBase<TestBase, TestBase> { }

[TypeOption(typeof(Tests), ""LocalOption"")]
public sealed class LocalOption : TestBase
{
    public LocalOption() : base(1, ""LocalOption"") { }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunTypeOptionGenerator(source, outputKind: OutputKind.DynamicallyLinkedLibrary);

        diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();

        var generated = CompilationHelper.GetGeneratedOutput(compilation, "TypeOptionModuleInitializer.g.cs");
        generated.ShouldBeNull();

        var diagnostic = CompilationHelper.GetGeneratedOutput(compilation, "TypeOptionModuleInitializer.Diagnostics.g.cs");
        diagnostic.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void GeneratorCreatesInitializerInConsumingAssembly()
    {
        var librarySource = @"
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Library;

public abstract class ConnectionBase : TypeOptionBase<int, ConnectionBase>
{
    protected ConnectionBase(int id, string name) : base(id, name) { }
}

[TypeCollection(typeof(ConnectionBase), typeof(ConnectionBase), typeof(Connections))]
public abstract partial class Connections : TypeCollectionBase<ConnectionBase, ConnectionBase> { }

[TypeOption(typeof(Connections), ""MsSql"")]
public sealed class MsSqlConnection : ConnectionBase
{
    public MsSqlConnection() : base(1, ""MsSql"") { }
}
";

        var libraryCompilation = CompilationHelper.CreateCompilation(librarySource);
        var libraryImage = CompilationHelper.CreateAssemblyImage(libraryCompilation);
        var libraryReference = MetadataReference.CreateFromImage(libraryImage);

        var consumingSource = @"
namespace Consumer;

public class Program
{
    public static void Main() { }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunTypeOptionGenerator(
            consumingSource,
            new[] { libraryReference });

        diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();

        var generated = CompilationHelper.GetGeneratedOutput(compilation, "TypeOptionModuleInitializer.g.cs");
        generated.ShouldNotBeNull();
        generated.ShouldContain("ModuleInitializer");
        generated.ShouldContain("Connections.RegisterMember");
        generated.ShouldContain("MsSqlConnection");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void GeneratorHandlesMultipleCollections()
    {
        var librarySource = @"
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Library;

public abstract class ConnectionBase : TypeOptionBase<int, ConnectionBase>
{
    protected ConnectionBase(int id, string name) : base(id, name) { }
}

[TypeCollection(typeof(ConnectionBase), typeof(ConnectionBase), typeof(Connections))]
public abstract partial class Connections : TypeCollectionBase<ConnectionBase, ConnectionBase> { }

public abstract class FilterBase : TypeOptionBase<int, FilterBase>
{
    protected FilterBase(int id, string name) : base(id, name) { }
}

[TypeCollection(typeof(FilterBase), typeof(FilterBase), typeof(Filters))]
public abstract partial class Filters : TypeCollectionBase<FilterBase, FilterBase> { }

[TypeOption(typeof(Connections), ""MsSql"")]
public sealed class MsSqlConnection : ConnectionBase
{
    public MsSqlConnection() : base(1, ""MsSql"") { }
}

[TypeOption(typeof(Filters), ""Equal"")]
public sealed class EqualFilter : FilterBase
{
    public EqualFilter() : base(1, ""Equal"") { }
}
";

        var libraryCompilation = CompilationHelper.CreateCompilation(librarySource);
        var libraryImage = CompilationHelper.CreateAssemblyImage(libraryCompilation);
        var libraryReference = MetadataReference.CreateFromImage(libraryImage);

        var consumingSource = @"
namespace Consumer;

public class Program
{
    public static void Main() { }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunTypeOptionGenerator(
            consumingSource,
            new[] { libraryReference });

        diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();

        var generated = CompilationHelper.GetGeneratedOutput(compilation, "TypeOptionModuleInitializer.g.cs");
        generated.ShouldNotBeNull();
        generated.ShouldContain("Connections.RegisterMember");
        generated.ShouldContain("MsSqlConnection");
        generated.ShouldContain("Filters.RegisterMember");
        generated.ShouldContain("EqualFilter");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void GeneratorRespectsRestrictToCurrentCompilationOnTypeOption()
    {
        var librarySource = @"
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Library;

public abstract class TestBase : TypeOptionBase<int, TestBase>
{
    protected TestBase(int id, string name) : base(id, name) { }
}

[TypeCollection(typeof(TestBase), typeof(TestBase), typeof(Tests))]
public abstract partial class Tests : TypeCollectionBase<TestBase, TestBase> { }

[TypeOption(typeof(Tests), ""Public"")]
public sealed class PublicOption : TestBase
{
    public PublicOption() : base(1, ""Public"") { }
}

[TypeOption(typeof(Tests), ""Restricted"", RestrictToCurrentCompilation = true)]
public sealed class RestrictedOption : TestBase
{
    public RestrictedOption() : base(2, ""Restricted"") { }
}
";

        var libraryCompilation = CompilationHelper.CreateCompilation(librarySource);
        var libraryImage = CompilationHelper.CreateAssemblyImage(libraryCompilation);
        var libraryReference = MetadataReference.CreateFromImage(libraryImage);

        var consumingSource = @"
namespace Consumer;

public class Program
{
    public static void Main() { }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunTypeOptionGenerator(
            consumingSource,
            new[] { libraryReference });

        diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();

        var generated = CompilationHelper.GetGeneratedOutput(compilation, "TypeOptionModuleInitializer.g.cs");
        generated.ShouldNotBeNull();
        generated.ShouldContain("PublicOption");
        generated.ShouldNotContain("RestrictedOption");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void GeneratorRespectsRestrictToCurrentCompilationOnTypeCollection()
    {
        var librarySource = @"
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Library;

public abstract class TestBase : TypeOptionBase<int, TestBase>
{
    protected TestBase(int id, string name) : base(id, name) { }
}

[TypeCollection(typeof(TestBase), typeof(TestBase), typeof(Tests), RestrictToCurrentCompilation = true)]
public abstract partial class Tests : TypeCollectionBase<TestBase, TestBase> { }

[TypeOption(typeof(Tests), ""Local"")]
public sealed class LocalOption : TestBase
{
    public LocalOption() : base(1, ""Local"") { }
}
";

        var libraryCompilation = CompilationHelper.CreateCompilation(librarySource);
        var libraryImage = CompilationHelper.CreateAssemblyImage(libraryCompilation);
        var libraryReference = MetadataReference.CreateFromImage(libraryImage);

        var consumingSource = @"
namespace Consumer;

public class Program
{
    public static void Main() { }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunTypeOptionGenerator(
            consumingSource,
            new[] { libraryReference });

        diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();

        // The generator may produce output for other TypeOptions in the fixed Fdw.Configuration
        // references (e.g. EnvironmentTypes, ForeignKeyActions). The important assertion is that
        // LocalOption — whose collection has RestrictToCurrentCompilation = true — is excluded.
        var generated = CompilationHelper.GetGeneratedOutput(compilation, "TypeOptionModuleInitializer.g.cs");
        generated?.ShouldNotContain("LocalOption");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void GeneratorSkipsAbstractTypes()
    {
        var librarySource = @"
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Library;

public abstract class TestBase : TypeOptionBase<int, TestBase>
{
    protected TestBase(int id, string name) : base(id, name) { }
}

[TypeCollection(typeof(TestBase), typeof(TestBase), typeof(Tests))]
public abstract partial class Tests : TypeCollectionBase<TestBase, TestBase> { }

[TypeOption(typeof(Tests), ""Abstract"")]
public abstract class AbstractOption : TestBase
{
    protected AbstractOption() : base(1, ""Abstract"") { }
}

[TypeOption(typeof(Tests), ""Concrete"")]
public sealed class ConcreteOption : TestBase
{
    public ConcreteOption() : base(2, ""Concrete"") { }
}
";

        var libraryCompilation = CompilationHelper.CreateCompilation(librarySource);
        var libraryImage = CompilationHelper.CreateAssemblyImage(libraryCompilation);
        var libraryReference = MetadataReference.CreateFromImage(libraryImage);

        var consumingSource = @"
namespace Consumer;

public class Program
{
    public static void Main() { }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunTypeOptionGenerator(
            consumingSource,
            new[] { libraryReference });

        diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();

        var generated = CompilationHelper.GetGeneratedOutput(compilation, "TypeOptionModuleInitializer.g.cs");
        generated.ShouldNotBeNull();
        generated.ShouldContain("ConcreteOption");
        generated.ShouldNotContain("AbstractOption");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void GeneratorSkipsGenericTypes()
    {
        var librarySource = @"
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Library;

public abstract class TestBase : TypeOptionBase<int, TestBase>
{
    protected TestBase(int id, string name) : base(id, name) { }
}

[TypeCollection(typeof(TestBase), typeof(TestBase), typeof(Tests))]
public abstract partial class Tests : TypeCollectionBase<TestBase, TestBase> { }

[TypeOption(typeof(Tests), ""Generic"")]
public sealed class GenericOption<T> : TestBase
{
    public GenericOption() : base(1, ""Generic"") { }
}

[TypeOption(typeof(Tests), ""Concrete"")]
public sealed class ConcreteOption : TestBase
{
    public ConcreteOption() : base(2, ""Concrete"") { }
}
";

        var libraryCompilation = CompilationHelper.CreateCompilation(librarySource);
        var libraryImage = CompilationHelper.CreateAssemblyImage(libraryCompilation);
        var libraryReference = MetadataReference.CreateFromImage(libraryImage);

        var consumingSource = @"
namespace Consumer;

public class Program
{
    public static void Main() { }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunTypeOptionGenerator(
            consumingSource,
            new[] { libraryReference });

        diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();

        var generated = CompilationHelper.GetGeneratedOutput(compilation, "TypeOptionModuleInitializer.g.cs");
        generated.ShouldNotBeNull();
        generated.ShouldContain("ConcreteOption");
        generated.ShouldNotContain("GenericOption");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void GeneratorAlwaysCreatesDiagnosticFile()
    {
        var source = @"
namespace Consumer;

public class Program
{
    public static void Main() { }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunTypeOptionGenerator(source);

        diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();

        var diagnostic = CompilationHelper.GetGeneratedOutput(compilation, "TypeOptionModuleInitializer.Diagnostics.g.cs");
        diagnostic.ShouldNotBeNull();
        diagnostic.ShouldContain("TypeOptionModuleInitializerGenerator");
    }
}
