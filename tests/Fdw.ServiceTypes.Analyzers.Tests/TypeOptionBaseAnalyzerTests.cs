using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    Fdw.ServiceTypes.Analyzers.TypeOptionBaseAnalyzer,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Fdw.ServiceTypes.Analyzers.Tests;

public class TypeOptionBaseAnalyzerTests
{

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task EnumOptionBase_WithIEnumOption_NoDiagnostic()
    {
        var test = """
            using System;

            namespace Fdw
            {
                public interface IEnumOption
                {
                    string Name { get; }
                }
            }

            namespace Fdw.ServiceTypes
            {
                [AttributeUsage(AttributeTargets.Class)]
                public class EnhancedEnumBaseAttribute : Attribute { }
            }

            namespace TestNamespace
            {
                using Fdw;
                using Fdw.ServiceTypes;

                [EnhancedEnumBase]
                public abstract class StatusBase : IEnumOption
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
    public async Task NonEnhancedEnum_WithoutIEnumOption_NoDiagnostic()
    {
        var test = """
            namespace TestNamespace
            {
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
    public async Task EnumOptionBase_ImplementsIEnumOptionThroughBaseClass_NoDiagnostic()
    {
        var test = """
            using System;

            namespace Fdw
            {
                public interface IEnumOption
                {
                    string Name { get; }
                }
            }

            namespace Fdw.ServiceTypes
            {
                [AttributeUsage(AttributeTargets.Class)]
                public class EnhancedEnumBaseAttribute : Attribute { }

                public abstract class BaseEnumOption : IEnumOption
                {
                    public abstract string Name { get; }
                }
            }

            namespace TestNamespace
            {
                using Fdw.ServiceTypes;

                [EnhancedEnumBase]
                public abstract class StatusBase : BaseEnumOption
                {
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task EnumOptionBase_WithIEnumOptionNotAvailable_NoDiagnostic()
    {
        // When IEnumOption is not in the compilation, analyzer should not report
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

        // This should NOT report a diagnostic because IEnumOption is not available
        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task ConcreteClass_WithEnhancedEnumBase_WithIEnumOption_NoDiagnostic()
    {
        var test = """
            using System;

            namespace Fdw
            {
                public interface IEnumOption
                {
                    string Name { get; }
                }
            }

            namespace Fdw.ServiceTypes
            {
                [AttributeUsage(AttributeTargets.Class)]
                public class EnhancedEnumBaseAttribute : Attribute { }
            }

            namespace TestNamespace
            {
                using Fdw;
                using Fdw.ServiceTypes;

                [EnhancedEnumBase]
                public class ConcreteStatus : IEnumOption
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
    public async Task EnumOptionBase_MultipleInterfaces_IncludingIEnumOption_NoDiagnostic()
    {
        var test = """
            using System;

            namespace Fdw
            {
                public interface IEnumOption
                {
                    string Name { get; }
                }

                public interface IOtherInterface
                {
                    int Value { get; }
                }
            }

            namespace Fdw.ServiceTypes
            {
                [AttributeUsage(AttributeTargets.Class)]
                public class EnhancedEnumBaseAttribute : Attribute { }
            }

            namespace TestNamespace
            {
                using Fdw;
                using Fdw.ServiceTypes;

                [EnhancedEnumBase]
                public abstract class StatusBase : IEnumOption, IOtherInterface
                {
                    public abstract string Name { get; }
                    public abstract int Value { get; }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }
}
