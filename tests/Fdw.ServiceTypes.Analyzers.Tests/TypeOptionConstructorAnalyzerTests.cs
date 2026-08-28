using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    Fdw.ServiceTypes.Analyzers.TypeOptionConstructorAnalyzer,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Fdw.ServiceTypes.Analyzers.Tests;

public class TypeOptionConstructorAnalyzerTests
{
    private const string TypeOptionAttributeDeclaration = """
        using System;

        namespace Fdw.Collections.Attributes
        {
            [AttributeUsage(AttributeTargets.Class)]
            public class TypeOptionAttribute : Attribute
            {
                public TypeOptionAttribute(Type collectionType, string name) { }

                public bool RestrictToCurrentCompilation { get; set; }
            }
        }

        """;

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public async Task TypeOption_WithoutParameterlessConstructor_ReportsDiagnostic()
    {
        var test = TypeOptionAttributeDeclaration + """
            namespace TestNamespace
            {
                using Fdw.Collections.Attributes;

                public abstract class StatusBase { }

                [TypeOption(typeof(StatusBase), "Active")]
                public class {|#0:ActiveStatus|} : StatusBase
                {
                    public ActiveStatus(string name)
                    {
                        Name = name;
                    }

                    public string Name { get; }
                }
            }
            """;

        var expected = VerifyCS.Diagnostic("FDW027")
            .WithLocation(0)
            .WithArguments("ActiveStatus");

        await VerifyCS.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public async Task TypeOption_WithParameterlessConstructor_NoDiagnostic()
    {
        var test = TypeOptionAttributeDeclaration + """
            namespace TestNamespace
            {
                using Fdw.Collections.Attributes;

                public abstract class StatusBase { }

                [TypeOption(typeof(StatusBase), "Active")]
                public class ActiveStatus : StatusBase
                {
                    public ActiveStatus() { }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task TypeOption_ImplicitParameterlessConstructor_NoDiagnostic()
    {
        var test = TypeOptionAttributeDeclaration + """
            namespace TestNamespace
            {
                using Fdw.Collections.Attributes;

                public abstract class StatusBase { }

                [TypeOption(typeof(StatusBase), "Active")]
                public class ActiveStatus : StatusBase
                {
                    public string Name { get; set; } = string.Empty;
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task TypeOption_WithoutAttribute_NoDiagnostic()
    {
        var test = """
            namespace TestNamespace
            {
                public abstract class StatusBase { }

                public class ActiveStatus : StatusBase
                {
                    public ActiveStatus(string name)
                    {
                        Name = name;
                    }

                    public string Name { get; }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task RecordTypeOption_WithPrimaryConstructor_WithoutParameterless_ReportsDiagnostic()
    {
        var test = TypeOptionAttributeDeclaration + """
            namespace TestNamespace
            {
                using Fdw.Collections.Attributes;

                public abstract record StatusBase { }

                [TypeOption(typeof(StatusBase), "Active")]
                public record {|#0:ActiveStatus|}(string Name) : StatusBase;
            }
            """;

        var expected = VerifyCS.Diagnostic("FDW027")
            .WithLocation(0)
            .WithArguments("ActiveStatus");

        await VerifyCS.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task TypeOption_AbstractClass_NoDiagnostic()
    {
        var test = TypeOptionAttributeDeclaration + """
            namespace TestNamespace
            {
                using Fdw.Collections.Attributes;

                public abstract class StatusBase { }

                [TypeOption(typeof(StatusBase), "Active")]
                public abstract class ActiveStatus : StatusBase
                {
                    protected ActiveStatus(string name)
                    {
                        Name = name;
                    }

                    public string Name { get; }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public async Task TypeOption_RestrictToCurrentCompilation_WithOptionalOnlyConstructor_NoDiagnostic()
    {
        var test = TypeOptionAttributeDeclaration + """
            namespace TestNamespace
            {
                using Fdw.Collections.Attributes;

                public abstract class StatusBase { }

                [TypeOption(typeof(StatusBase), "Active", RestrictToCurrentCompilation = true)]
                public class ActiveStatus : StatusBase
                {
                    public ActiveStatus(object? logger = null) { }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task TypeOption_GenericClass_NoDiagnostic()
    {
        var test = TypeOptionAttributeDeclaration + """
            namespace TestNamespace
            {
                using Fdw.Collections.Attributes;

                public abstract class StatusBase<T> { }

                [TypeOption(typeof(StatusBase<>), "Active")]
                public class ActiveStatus<T> : StatusBase<T>
                {
                    public ActiveStatus(T value)
                    {
                        Value = value;
                    }

                    public T Value { get; }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }
}
