using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    Fdw.ServiceTypes.Analyzers.AbstractMemberAnalyzer,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Fdw.ServiceTypes.Analyzers.Tests;

public class AbstractMemberAnalyzerTests
{

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task AbstractProperty_InEnumCollection_ReportsDiagnostic()
    {
        var test = """
            using System;

            namespace Fdw.ServiceTypes
            {
                [AttributeUsage(AttributeTargets.Class)]
                public class ServiceServiceTypeCollectionAttribute : Attribute { }
            }

            namespace TestNamespace
            {
                using Fdw.ServiceTypes;

                [ServiceServiceTypeCollection]
                public abstract class TestEnumBase
                {
                    public abstract string {|#0:Name|} { get; }
                }
            }
            """;

        var expected = VerifyCS.Diagnostic("FDW028")
            .WithLocation(0)
            .WithArguments("Name");

        await VerifyCS.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task AbstractProperty_WithEnumCollectionAlias_ReportsDiagnostic()
    {
        var test = """
            using System;

            namespace Fdw.ServiceTypes
            {
                [AttributeUsage(AttributeTargets.Class)]
                public class EnumCollectionAttribute : Attribute { }
            }

            namespace TestNamespace
            {
                using Fdw.ServiceTypes;

                [EnumCollection]
                public abstract class TestEnumBase
                {
                    public abstract int {|#0:Value|} { get; }
                }
            }
            """;

        var expected = VerifyCS.Diagnostic("FDW028")
            .WithLocation(0)
            .WithArguments("Value");

        await VerifyCS.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task VirtualProperty_InEnumCollection_NoDiagnostic()
    {
        var test = """
            using System;

            namespace Fdw.ServiceTypes
            {
                [AttributeUsage(AttributeTargets.Class)]
                public class ServiceServiceTypeCollectionAttribute : Attribute { }
            }

            namespace TestNamespace
            {
                using Fdw.ServiceTypes;

                [ServiceServiceTypeCollection]
                public abstract class TestEnumBase
                {
                    public virtual string Name { get; protected set; } = string.Empty;
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task AbstractProperty_WithoutEnumCollectionAttribute_NoDiagnostic()
    {
        var test = """
            namespace TestNamespace
            {
                public abstract class TestClass
                {
                    public abstract string Name { get; }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task MultipleAbstractProperties_InEnumCollection_ReportsAllDiagnostics()
    {
        var test = """
            using System;

            namespace Fdw.ServiceTypes
            {
                [AttributeUsage(AttributeTargets.Class)]
                public class ServiceServiceTypeCollectionAttribute : Attribute { }
            }

            namespace TestNamespace
            {
                using Fdw.ServiceTypes;

                [ServiceServiceTypeCollection]
                public abstract class TestEnumBase
                {
                    public abstract string {|#0:Name|} { get; }
                    public abstract int {|#1:Value|} { get; }
                    public abstract bool {|#2:IsActive|} { get; }
                }
            }
            """;

        var expected = new[]
        {
            VerifyCS.Diagnostic("FDW028").WithLocation(0).WithArguments("Name"),
            VerifyCS.Diagnostic("FDW028").WithLocation(1).WithArguments("Value"),
            VerifyCS.Diagnostic("FDW028").WithLocation(2).WithArguments("IsActive")
        };

        await VerifyCS.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task AbstractField_InEnumCollection_ReportsError()
    {
        var test = """
            using System;

            namespace Fdw.ServiceTypes
            {
                [AttributeUsage(AttributeTargets.Class)]
                public class ServiceServiceTypeCollectionAttribute : Attribute { }
            }

            namespace TestNamespace
            {
                using Fdw.ServiceTypes;

                [ServiceServiceTypeCollection]
                public abstract class TestEnumBase
                {
                    public abstract string {|#0:_name|};
                }
            }
            """;

        var expected = VerifyCS.Diagnostic("FDW029")
            .WithLocation(0)
            .WithArguments("_name");

        var compilerError = DiagnosticResult.CompilerError("CS0681")
            .WithSpan(16, 32, 16, 37);

        await VerifyCS.VerifyAnalyzerAsync(test, expected, compilerError);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task ConcreteProperty_InEnumCollection_NoDiagnostic()
    {
        var test = """
            using System;

            namespace Fdw.ServiceTypes
            {
                [AttributeUsage(AttributeTargets.Class)]
                public class ServiceServiceTypeCollectionAttribute : Attribute { }
            }

            namespace TestNamespace
            {
                using Fdw.ServiceTypes;

                [ServiceServiceTypeCollection]
                public abstract class TestEnumBase
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
    public async Task AbstractProperty_InNestedEnumCollection_ReportsDiagnostic()
    {
        var test = """
            using System;

            namespace Fdw.ServiceTypes
            {
                [AttributeUsage(AttributeTargets.Class)]
                public class ServiceServiceTypeCollectionAttribute : Attribute { }
            }

            namespace TestNamespace
            {
                using Fdw.ServiceTypes;

                public class OuterClass
                {
                    [ServiceServiceTypeCollection]
                    public abstract class TestEnumBase
                    {
                        public abstract string {|#0:Name|} { get; }
                    }
                }
            }
            """;

        var expected = VerifyCS.Diagnostic("FDW028")
            .WithLocation(0)
            .WithArguments("Name");

        await VerifyCS.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task MixedAbstractAndVirtualProperties_InEnumCollection_ReportsOnlyAbstract()
    {
        var test = """
            using System;

            namespace Fdw.ServiceTypes
            {
                [AttributeUsage(AttributeTargets.Class)]
                public class ServiceServiceTypeCollectionAttribute : Attribute { }
            }

            namespace TestNamespace
            {
                using Fdw.ServiceTypes;

                [ServiceServiceTypeCollection]
                public abstract class TestEnumBase
                {
                    public abstract string {|#0:Name|} { get; }
                    public virtual int Value { get; set; }
                    public abstract bool {|#1:IsActive|} { get; }
                }
            }
            """;

        var expected = new[]
        {
            VerifyCS.Diagnostic("FDW028").WithLocation(0).WithArguments("Name"),
            VerifyCS.Diagnostic("FDW028").WithLocation(1).WithArguments("IsActive")
        };

        await VerifyCS.VerifyAnalyzerAsync(test, expected);
    }
}
