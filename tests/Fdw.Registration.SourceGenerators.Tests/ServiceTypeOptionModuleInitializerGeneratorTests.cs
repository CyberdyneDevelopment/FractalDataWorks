using System.Linq;
using Microsoft.CodeAnalysis;
using Shouldly;
using Xunit;

namespace Fdw.Registration.SourceGenerators.Tests;

public class ServiceTypeOptionModuleInitializerGeneratorTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void GeneratorSkipsAssemblyDefiningServiceTypeOptions()
    {
        var source = @"
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Test;

public abstract class ServiceBase : TypeOptionBase<int, ServiceBase>
{
    protected ServiceBase(int id, string name) : base(id, name) { }
}

[ServiceTypeCollection(typeof(ServiceBase), typeof(ServiceBase), typeof(Services))]
public abstract partial class Services : TypeCollectionBase<ServiceBase, ServiceBase> { }

[ServiceTypeOption(typeof(Services), ""LocalService"")]
public sealed class LocalService : ServiceBase
{
    public LocalService() : base(1, ""LocalService"") { }
}
";

        var (compilation, diagnostics) = CompilationHelper.RunServiceTypeOptionGenerator(source, outputKind: OutputKind.DynamicallyLinkedLibrary);

        diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();

        var generated = CompilationHelper.GetGeneratedOutput(compilation, "ServiceTypeOptionModuleInitializer.g.cs");
        generated.ShouldBeNull();

        var diagnostic = CompilationHelper.GetGeneratedOutput(compilation, "ServiceTypeOptionModuleInitializer.Diagnostics.g.cs");
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

[ServiceTypeCollection(typeof(ConnectionBase), typeof(ConnectionBase), typeof(Connections))]
public abstract partial class Connections : TypeCollectionBase<ConnectionBase, ConnectionBase> { }

[ServiceTypeOption(typeof(Connections), ""MsSql"")]
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

        var (compilation, diagnostics) = CompilationHelper.RunServiceTypeOptionGenerator(
            consumingSource,
            new[] { libraryReference });

        diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();

        var generated = CompilationHelper.GetGeneratedOutput(compilation, "ServiceTypeOptionModuleInitializer.g.cs");
        generated.ShouldNotBeNull();
        generated.ShouldContain("ModuleInitializer");
        generated.ShouldContain("Connections.RegisterMember");
        generated.ShouldContain("MsSqlConnection");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void GeneratorHandlesMultipleServiceTypes()
    {
        var librarySource = @"
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Library;

public abstract class ConnectionBase : TypeOptionBase<int, ConnectionBase>
{
    protected ConnectionBase(int id, string name) : base(id, name) { }
}

[ServiceTypeCollection(typeof(ConnectionBase), typeof(ConnectionBase), typeof(Connections))]
public abstract partial class Connections : TypeCollectionBase<ConnectionBase, ConnectionBase> { }

public abstract class AuthBase : TypeOptionBase<int, AuthBase>
{
    protected AuthBase(int id, string name) : base(id, name) { }
}

[ServiceTypeCollection(typeof(AuthBase), typeof(AuthBase), typeof(AuthProviders))]
public abstract partial class AuthProviders : TypeCollectionBase<AuthBase, AuthBase> { }

[ServiceTypeOption(typeof(Connections), ""MsSql"")]
public sealed class MsSqlConnection : ConnectionBase
{
    public MsSqlConnection() : base(1, ""MsSql"") { }
}

[ServiceTypeOption(typeof(AuthProviders), ""OAuth"")]
public sealed class OAuthProvider : AuthBase
{
    public OAuthProvider() : base(1, ""OAuth"") { }
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

        var (compilation, diagnostics) = CompilationHelper.RunServiceTypeOptionGenerator(
            consumingSource,
            new[] { libraryReference });

        diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();

        var generated = CompilationHelper.GetGeneratedOutput(compilation, "ServiceTypeOptionModuleInitializer.g.cs");
        generated.ShouldNotBeNull();
        generated.ShouldContain("Connections.RegisterMember");
        generated.ShouldContain("MsSqlConnection");
        generated.ShouldContain("AuthProviders.RegisterMember");
        generated.ShouldContain("OAuthProvider");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void GeneratorSkipsAbstractServiceTypes()
    {
        var librarySource = @"
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Library;

public abstract class ServiceBase : TypeOptionBase<int, ServiceBase>
{
    protected ServiceBase(int id, string name) : base(id, name) { }
}

[ServiceTypeCollection(typeof(ServiceBase), typeof(ServiceBase), typeof(Services))]
public abstract partial class Services : TypeCollectionBase<ServiceBase, ServiceBase> { }

[ServiceTypeOption(typeof(Services), ""Abstract"")]
public abstract class AbstractService : ServiceBase
{
    protected AbstractService() : base(1, ""Abstract"") { }
}

[ServiceTypeOption(typeof(Services), ""Concrete"")]
public sealed class ConcreteService : ServiceBase
{
    public ConcreteService() : base(2, ""Concrete"") { }
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

        var (compilation, diagnostics) = CompilationHelper.RunServiceTypeOptionGenerator(
            consumingSource,
            new[] { libraryReference });

        diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();

        var generated = CompilationHelper.GetGeneratedOutput(compilation, "ServiceTypeOptionModuleInitializer.g.cs");
        generated.ShouldNotBeNull();
        generated.ShouldContain("ConcreteService");
        generated.ShouldNotContain("AbstractService");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void GeneratorSkipsGenericServiceTypes()
    {
        var librarySource = @"
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Library;

public abstract class ServiceBase : TypeOptionBase<int, ServiceBase>
{
    protected ServiceBase(int id, string name) : base(id, name) { }
}

[ServiceTypeCollection(typeof(ServiceBase), typeof(ServiceBase), typeof(Services))]
public abstract partial class Services : TypeCollectionBase<ServiceBase, ServiceBase> { }

[ServiceTypeOption(typeof(Services), ""Generic"")]
public sealed class GenericService<T> : ServiceBase
{
    public GenericService() : base(1, ""Generic"") { }
}

[ServiceTypeOption(typeof(Services), ""Concrete"")]
public sealed class ConcreteService : ServiceBase
{
    public ConcreteService() : base(2, ""Concrete"") { }
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

        var (compilation, diagnostics) = CompilationHelper.RunServiceTypeOptionGenerator(
            consumingSource,
            new[] { libraryReference });

        diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();

        var generated = CompilationHelper.GetGeneratedOutput(compilation, "ServiceTypeOptionModuleInitializer.g.cs");
        generated.ShouldNotBeNull();
        generated.ShouldContain("ConcreteService");
        generated.ShouldNotContain("GenericService");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void GeneratorRequiresParameterlessConstructor()
    {
        var librarySource = @"
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Library;

public abstract class ServiceBase : TypeOptionBase<int, ServiceBase>
{
    protected ServiceBase(int id, string name) : base(id, name) { }
}

[ServiceTypeCollection(typeof(ServiceBase), typeof(ServiceBase), typeof(Services))]
public abstract partial class Services : TypeCollectionBase<ServiceBase, ServiceBase> { }

[ServiceTypeOption(typeof(Services), ""NoParameterless"")]
public sealed class NoParameterlessService : ServiceBase
{
    public NoParameterlessService(string param) : base(1, ""NoParameterless"") { }
}

[ServiceTypeOption(typeof(Services), ""Valid"")]
public sealed class ValidService : ServiceBase
{
    public ValidService() : base(2, ""Valid"") { }
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

        var (compilation, diagnostics) = CompilationHelper.RunServiceTypeOptionGenerator(
            consumingSource,
            new[] { libraryReference });

        diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();

        var generated = CompilationHelper.GetGeneratedOutput(compilation, "ServiceTypeOptionModuleInitializer.g.cs");
        generated.ShouldNotBeNull();
        generated.ShouldContain("ValidService");
        generated.ShouldNotContain("NoParameterlessService");
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

        var (compilation, diagnostics) = CompilationHelper.RunServiceTypeOptionGenerator(source);

        diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();

        var diagnostic = CompilationHelper.GetGeneratedOutput(compilation, "ServiceTypeOptionModuleInitializer.Diagnostics.g.cs");
        diagnostic.ShouldNotBeNull();
        diagnostic.ShouldContain("ServiceTypeOptionModuleInitializerGenerator");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void GeneratorUsesFullyQualifiedNames()
    {
        var librarySource = @"
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Library.Services;

public abstract class ConnectionBase : TypeOptionBase<int, ConnectionBase>
{
    protected ConnectionBase(int id, string name) : base(id, name) { }
}

[ServiceTypeCollection(typeof(ConnectionBase), typeof(ConnectionBase), typeof(Connections))]
public abstract partial class Connections : TypeCollectionBase<ConnectionBase, ConnectionBase> { }

[ServiceTypeOption(typeof(Connections), ""MsSql"")]
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

        var (compilation, diagnostics) = CompilationHelper.RunServiceTypeOptionGenerator(
            consumingSource,
            new[] { libraryReference });

        diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();

        var generated = CompilationHelper.GetGeneratedOutput(compilation, "ServiceTypeOptionModuleInitializer.g.cs");
        generated.ShouldNotBeNull();
        generated.ShouldContain("global::Library.Services.Connections.RegisterMember");
        generated.ShouldContain("global::Library.Services.MsSqlConnection");
    }
}
