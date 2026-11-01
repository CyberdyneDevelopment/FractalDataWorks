using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    Fdw.ServiceTypes.Analyzers.TypeOptionBaseConstructorAnalyzer,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Fdw.ServiceTypes.Analyzers.Tests;

public class TypeOptionBaseConstructorAnalyzerTests
{

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task AbstractEnumBase_WithAbstractProperties_WithoutConstructor_ReportsDiagnostic()
    {
        var test = """
            using System;

            namespace Fdw.ServiceTypes
            {
                [AttributeUsage(AttributeTargets.Class)]
                public class EnhancedEnumBaseAttribute : Attribute { }
            }

            namespace TestNamespace
            {
                using Fdw.ServiceTypes;

                [EnhancedEnumBase]
                public abstract class {|#0:StatusBase|}
                {
                    public abstract int Code { get; }
                }
            }
            """;

        var expected = VerifyCS.Diagnostic("FDW034")
            .WithLocation(0)
            .WithArguments("StatusBase");

        await VerifyCS.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task AbstractEnumBase_WithAbstractProperties_WithConstructor_NoDiagnostic()
    {
        var test = """
            using System;

            namespace Fdw.ServiceTypes
            {
                [AttributeUsage(AttributeTargets.Class)]
                public class EnhancedEnumBaseAttribute : Attribute { }
            }

            namespace TestNamespace
            {
                using Fdw.ServiceTypes;

                [EnhancedEnumBase]
                public abstract class StatusBase
                {
                    protected StatusBase(int code)
                    {
                        Code = code;
                    }

                    public int Code { get; }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task AbstractEnumBase_OnlyNameAbstract_WithoutConstructor_NoDiagnostic()
    {
        var test = """
            using System;

            namespace Fdw.ServiceTypes
            {
                [AttributeUsage(AttributeTargets.Class)]
                public class EnhancedEnumBaseAttribute : Attribute { }
            }

            namespace TestNamespace
            {
                using Fdw.ServiceTypes;

                [EnhancedEnumBase]
                public abstract class StatusBase
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
    public async Task ConcreteEnumBase_WithAbstractProperties_NoDiagnostic()
    {
        var test = """
            using System;

            namespace Fdw.ServiceTypes
            {
                [AttributeUsage(AttributeTargets.Class)]
                public class EnhancedEnumBaseAttribute : Attribute { }
            }

            namespace TestNamespace
            {
                using Fdw.ServiceTypes;

                [EnhancedEnumBase]
                public class StatusBase
                {
                    public int Code { get; set; }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task AbstractEnumBase_WithVirtualProperties_NoDiagnostic()
    {
        var test = """
            using System;

            namespace Fdw.ServiceTypes
            {
                [AttributeUsage(AttributeTargets.Class)]
                public class EnhancedEnumBaseAttribute : Attribute { }
            }

            namespace TestNamespace
            {
                using Fdw.ServiceTypes;

                [EnhancedEnumBase]
                public abstract class StatusBase
                {
                    public virtual int Code { get; protected set; }
                    public virtual string Category { get; protected set; } = string.Empty;
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task AbstractEnumBase_WithPrivateConstructor_ReportsDiagnostic()
    {
        var test = """
            using System;

            namespace Fdw.ServiceTypes
            {
                [AttributeUsage(AttributeTargets.Class)]
                public class EnhancedEnumBaseAttribute : Attribute { }
            }

            namespace TestNamespace
            {
                using Fdw.ServiceTypes;

                [EnhancedEnumBase]
                public abstract class {|#0:StatusBase|}
                {
                    private StatusBase() { }

                    public abstract int Code { get; }
                }
            }
            """;

        var expected = VerifyCS.Diagnostic("FDW034")
            .WithLocation(0)
            .WithArguments("StatusBase");

        await VerifyCS.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task NonEnhancedEnumBase_WithAbstractProperties_NoDiagnostic()
    {
        var test = """
            namespace TestNamespace
            {
                public abstract class StatusBase
                {
                    public abstract int Code { get; }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task EnumOptionBaseAlias_WithAbstractProperties_WithoutConstructor_ReportsDiagnostic()
    {
        var test = """
            using System;

            namespace Fdw.ServiceTypes
            {
                [AttributeUsage(AttributeTargets.Class)]
                public class EnumOptionBaseAttribute : Attribute { }
            }

            namespace TestNamespace
            {
                using Fdw.ServiceTypes;

                [EnumOptionBase]
                public abstract class {|#0:ColorBase|}
                {
                    public abstract string HexCode { get; }
                }
            }
            """;

        var expected = VerifyCS.Diagnostic("FDW034")
            .WithLocation(0)
            .WithArguments("ColorBase");

        await VerifyCS.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task AbstractEnumBase_MultipleAbstractProperties_WithoutConstructor_ReportsDiagnostic()
    {
        var test = """
            using System;

            namespace Fdw.ServiceTypes
            {
                [AttributeUsage(AttributeTargets.Class)]
                public class EnhancedEnumBaseAttribute : Attribute { }
            }

            namespace TestNamespace
            {
                using Fdw.ServiceTypes;

                [EnhancedEnumBase]
                public abstract class {|#0:StatusBase|}
                {
                    public abstract int Code { get; }
                    public abstract string Category { get; }
                    public abstract bool IsActive { get; }
                }
            }
            """;

        var expected = VerifyCS.Diagnostic("FDW034")
            .WithLocation(0)
            .WithArguments("StatusBase");

        await VerifyCS.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task AbstractEnumBase_MixedAbstractAndVirtual_WithoutConstructor_ReportsDiagnostic()
    {
        var test = """
            using System;

            namespace Fdw.ServiceTypes
            {
                [AttributeUsage(AttributeTargets.Class)]
                public class EnhancedEnumBaseAttribute : Attribute { }
            }

            namespace TestNamespace
            {
                using Fdw.ServiceTypes;

                [EnhancedEnumBase]
                public abstract class {|#0:StatusBase|}
                {
                    public abstract int Code { get; }
                    public virtual string Category { get; protected set; } = string.Empty;
                }
            }
            """;

        var expected = VerifyCS.Diagnostic("FDW034")
            .WithLocation(0)
            .WithArguments("StatusBase");

        await VerifyCS.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task AbstractEnumBase_WithProtectedConstructor_NoDiagnostic()
    {
        var test = """
            using System;

            namespace Fdw.ServiceTypes
            {
                [AttributeUsage(AttributeTargets.Class)]
                public class EnhancedEnumBaseAttribute : Attribute { }
            }

            namespace TestNamespace
            {
                using Fdw.ServiceTypes;

                [EnhancedEnumBase]
                public abstract class StatusBase
                {
                    protected StatusBase(int code)
                    {
                        Code = code;
                    }

                    public int Code { get; }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }
}
