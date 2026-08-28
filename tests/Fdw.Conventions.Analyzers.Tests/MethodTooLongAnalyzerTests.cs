using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    Fdw.Conventions.Analyzers.MethodTooLongAnalyzer,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Fdw.Conventions.Analyzers.Tests;

/// <summary>
/// Tests for <see cref="MethodTooLongAnalyzer"/> (FDW006) — what counts as one method's length. The
/// distinction that carries the rule is between a body a method contains and a body it merely declares:
/// a lambda is measured as its own unit, not as part of whatever method wrote it down.
/// </summary>
public class MethodTooLongAnalyzerTests
{
    private const int Threshold = 60;

    private static string Filler(int count) =>
        string.Join(
            Environment.NewLine,
            Enumerable.Range(0, count).Select(index => $"        System.Console.WriteLine(\"{index}\");"));

    private const string AttributeFixture = """
        using System;

        namespace Fdw.Collections.Attributes
        {
            [AttributeUsage(AttributeTargets.Class)]
            public class ServiceTypeOptionAttribute : Attribute
            {
                public ServiceTypeOptionAttribute(string name) { }
            }
        }

        """;

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Analyzer")]
    public async Task MethodOverThreshold_ReportsDiagnostic()
    {
        var test = $$"""
            public class Sample
            {
                public void {|#0:LongMethod|}()
                {
            {{Filler(Threshold + 1)}}
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(
            test,
            VerifyCS.Diagnostic("FDW006").WithLocation(0).WithArguments("LongMethod", Threshold + 1, Threshold));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Analyzer")]
    public async Task MethodAtThreshold_ReportsNothing()
    {
        var test = $$"""
            public class Sample
            {
                public void ShortEnough()
                {
            {{Filler(Threshold)}}
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Analyzer")]
    public async Task LambdaBody_DoesNotCountAgainstTheMethodThatDeclaresIt()
    {
        var test = $$"""
            using System;

            public class Sample
            {
                public Sample()
                {
                    var first = 1;
                    var second = 2;
                    var third = 3;
                    Action body = () =>
                    {
            {{Filler(Threshold - 2)}}
                    };
                    body();
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Analyzer")]
    public async Task LambdaOverThreshold_IsReportedOnItsOwn()
    {
        var test = $$"""
            using System;

            public class Sample
            {
                public Sample()
                {
                    Action<int> body = {|#0:value|} =>
                    {
            {{Filler(Threshold + 1)}}
                    };
                    body(0);
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(
            test,
            VerifyCS.Diagnostic("FDW006").WithLocation(0).WithArguments("Sample (lambda)", Threshold + 1, Threshold));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Analyzer")]
    public async Task LambdaOverThreshold_InAttributedAuthoringClass_ReportsNothing()
    {
        var test = AttributeFixture + $$"""
            namespace TestNamespace
            {
                using System;
                using Fdw.Collections.Attributes;

                [ServiceTypeOption("MsSql")]
                public sealed class MsSqlConnectionType
                {
                    public MsSqlConnectionType()
                    {
                        Action<int> body = value =>
                        {
            {{Filler(Threshold + 1)}}
                        };
                        body(0);
                    }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Analyzer")]
    public async Task LambdaOverThreshold_InUnattributedClass_StillReports()
    {
        var test = AttributeFixture + $$"""
            namespace TestNamespace
            {
                using System;

                public sealed class Ordinary
                {
                    public Ordinary()
                    {
                        Action<int> body = {|#0:value|} =>
                        {
            {{Filler(Threshold + 1)}}
                        };
                        body(0);
                    }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(
            test,
            VerifyCS.Diagnostic("FDW006").WithLocation(0).WithArguments("Ordinary (lambda)", Threshold + 1, Threshold));
    }
}
