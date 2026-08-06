using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using Xunit;

namespace Fdw.MessageLogging.Generators.Tests;

public sealed class RoslynExtensionsTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GetBestTypeByMetadataNameReturnsTypeWhenFound()
    {
        string source = """
            namespace TestNamespace
            {
                public class TestClass { }
            }
            """;

        var compilation = CreateCompilation(source);

        var type = compilation.GetBestTypeByMetadataName("TestNamespace.TestClass");

        type.ShouldNotBeNull();
        type.Name.ShouldBe("TestClass");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GetBestTypeByMetadataNameReturnsNullWhenNotFound()
    {
        string source = """
            namespace TestNamespace
            {
                public class TestClass { }
            }
            """;

        var compilation = CreateCompilation(source);

        var type = compilation.GetBestTypeByMetadataName("TestNamespace.NonExistentClass");

        type.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GetBestTypeByMetadataNameFindsSystemTypes()
    {
        string source = "// Empty source";

        var compilation = CreateCompilation(source);

        var type = compilation.GetBestTypeByMetadataName("System.String");

        type.ShouldNotBeNull();
        type.SpecialType.ShouldBe(SpecialType.System_String);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GetBestTypeByMetadataNameFindsGenericTypes()
    {
        string source = """
            namespace TestNamespace
            {
                public class GenericClass<T> { }
            }
            """;

        var compilation = CreateCompilation(source);

        var type = compilation.GetBestTypeByMetadataName("TestNamespace.GenericClass`1");

        type.ShouldNotBeNull();
        type.Name.ShouldBe("GenericClass");
        type.Arity.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GetBestTypeByMetadataNameFindsNestedTypes()
    {
        string source = """
            namespace TestNamespace
            {
                public class OuterClass
                {
                    public class InnerClass { }
                }
            }
            """;

        var compilation = CreateCompilation(source);

        var type = compilation.GetBestTypeByMetadataName("TestNamespace.OuterClass+InnerClass");

        type.ShouldNotBeNull();
        type.Name.ShouldBe("InnerClass");
    }

    // Note: DiagnosticDescriptorHelper tests with LocalizableResourceString require a proper ResourceManager.
    // The DiagnosticDescriptorHelper functionality is fully covered by DiagnosticDescriptorsTests
    // which test all 27 actual diagnostic descriptors that use this helper.

    private static CSharpCompilation CreateCompilation(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
        };

        return CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}
