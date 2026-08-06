using System.Linq;
using Microsoft.CodeAnalysis;
using Shouldly;
using Xunit;

namespace Fdw.Registration.SourceGenerators.Tests;

public class PocoMapperModuleInitializerGeneratorTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void GeneratorSkipsAssemblyDefiningMappers()
    {
        var source = @"
using System;
using Fdw.Data;

namespace Test;

[GenerateMapper]
public sealed class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
";

        var (compilation, diagnostics) = CompilationHelper.RunPocoMapperGenerator(source, outputKind: OutputKind.DynamicallyLinkedLibrary);

        diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();

        var generated = CompilationHelper.GetGeneratedOutput(compilation, "PocoMapperModuleInitializer.g.cs");
        generated.ShouldBeNull();

        var diagnostic = CompilationHelper.GetGeneratedOutput(compilation, "PocoMapperModuleInitializer.Diagnostics.g.cs");
        diagnostic.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void GeneratorCreatesInitializerInConsumingAssembly()
    {
        var librarySource = @"
using System;
using Fdw.Data;

namespace Library;

[GenerateMapper]
public sealed class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
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

        var (compilation, diagnostics) = CompilationHelper.RunPocoMapperGenerator(
            consumingSource,
            new[] { libraryReference });

        diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();

        var generated = CompilationHelper.GetGeneratedOutput(compilation, "PocoMapperModuleInitializer.g.cs");
        generated.ShouldNotBeNull();
        generated.ShouldContain("ModuleInitializer");
        generated.ShouldContain("PocoMapperCollection.RegisterMember");
        generated.ShouldContain("UserDtoPocoMapper");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void GeneratorHandlesMultipleMappers()
    {
        var librarySource = @"
using System;
using Fdw.Data;

namespace Library;

[GenerateMapper]
public sealed class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

[GenerateMapper]
public sealed class ProductDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
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

        var (compilation, diagnostics) = CompilationHelper.RunPocoMapperGenerator(
            consumingSource,
            new[] { libraryReference });

        diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();

        var generated = CompilationHelper.GetGeneratedOutput(compilation, "PocoMapperModuleInitializer.g.cs");
        generated.ShouldNotBeNull();
        generated.ShouldContain("UserDtoPocoMapper");
        generated.ShouldContain("ProductDtoPocoMapper");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void GeneratorSkipsGenericTypes()
    {
        var librarySource = @"
using System;
using Fdw.Data;

namespace Library;

[GenerateMapper]
public sealed class GenericDto<T>
{
    public T Value { get; set; } = default!;
}

[GenerateMapper]
public sealed class ConcreteDto
{
    public int Value { get; set; }
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

        var (compilation, diagnostics) = CompilationHelper.RunPocoMapperGenerator(
            consumingSource,
            new[] { libraryReference });

        diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();

        var generated = CompilationHelper.GetGeneratedOutput(compilation, "PocoMapperModuleInitializer.g.cs");
        generated.ShouldNotBeNull();
        generated.ShouldContain("ConcreteDtoPocoMapper");
        generated.ShouldNotContain("GenericDtoPocoMapper");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void GeneratorHandlesNestedTypes()
    {
        var librarySource = @"
using System;
using Fdw.Data;

namespace Library;

public static class Models
{
    [GenerateMapper]
    public sealed class UserDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
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

        var (compilation, diagnostics) = CompilationHelper.RunPocoMapperGenerator(
            consumingSource,
            new[] { libraryReference });

        diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();

        var generated = CompilationHelper.GetGeneratedOutput(compilation, "PocoMapperModuleInitializer.g.cs");
        generated.ShouldNotBeNull();
        generated.ShouldContain("UserDtoPocoMapper");
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

        var (compilation, diagnostics) = CompilationHelper.RunPocoMapperGenerator(source);

        diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();

        var diagnostic = CompilationHelper.GetGeneratedOutput(compilation, "PocoMapperModuleInitializer.Diagnostics.g.cs");
        diagnostic.ShouldNotBeNull();
        diagnostic.ShouldContain("PocoMapperModuleInitializerGenerator");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void GeneratorUsesFullyQualifiedNames()
    {
        var librarySource = @"
using System;
using Fdw.Data;

namespace Library.Data.Models;

[GenerateMapper]
public sealed class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
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

        var (compilation, diagnostics) = CompilationHelper.RunPocoMapperGenerator(
            consumingSource,
            new[] { libraryReference });

        diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();

        var generated = CompilationHelper.GetGeneratedOutput(compilation, "PocoMapperModuleInitializer.g.cs");
        generated.ShouldNotBeNull();
        generated.ShouldContain("global::Library.Data.Models.UserDtoPocoMapper");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void GeneratorUsesCorrectMapperNamingConvention()
    {
        var librarySource = @"
using System;
using Fdw.Data;

namespace Library;

[GenerateMapper]
public sealed class Customer
{
    public int Id { get; set; }
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

        var (compilation, diagnostics) = CompilationHelper.RunPocoMapperGenerator(
            consumingSource,
            new[] { libraryReference });

        diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();

        var generated = CompilationHelper.GetGeneratedOutput(compilation, "PocoMapperModuleInitializer.g.cs");
        generated.ShouldNotBeNull();
        generated.ShouldContain("CustomerPocoMapper");
    }

    [Fact(Skip = "Complex multi-generator scenario - module initializer looks for generated mapper classes, not source types")]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void GeneratorHandlesMultipleAssemblies()
    {
        // This test is skipped because it requires running PocoMapperGenerator first to generate
        // the *PocoMapper classes, then compiling that output, then testing the module initializer.
        // Unit testing this scenario would require complex multi-stage compilation that's not practical.
        // The functionality is tested in integration tests where both generators run in sequence.
    }
}
