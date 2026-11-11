using System.Threading.Tasks;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    Fdw.Conventions.Analyzers.TypeCollectionOpportunityAnalyzer,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Fdw.Conventions.Analyzers.Tests;

/// <summary>
/// Tests for <see cref="TypeCollectionOpportunityAnalyzer"/> (FDW017, FDW018, FDW019). The rule is that no enum is defined outside the
/// TypeCollection layer, so a switch or an if/else chain is reported only over an enum the compilation defines in source. An enum of the
/// framework (a TypeCollection cannot replace it) is left alone (FDW-5264).
/// </summary>
public class TypeCollectionOpportunityAnalyzerTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Conventions")]
    public async Task SwitchStatementOverAFrameworkEnumReportsNothing()
    {
        var test = """
            using System.Reflection;

            namespace TestNamespace
            {
                public class Access
                {
                    public static string Visibility(MethodAttributes attributes)
                    {
                        switch (attributes & MethodAttributes.MemberAccessMask)
                        {
                            case MethodAttributes.Public: return "public";
                            case MethodAttributes.Private: return "private";
                            default: return "other";
                        }
                    }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Conventions")]
    public async Task SwitchExpressionOverAFrameworkEnumReportsNothing()
    {
        var test = """
            using System;

            namespace TestNamespace
            {
                public class Days
                {
                    public static string Name(DayOfWeek day) => day switch
                    {
                        DayOfWeek.Monday => "mon",
                        DayOfWeek.Tuesday => "tue",
                        _ => "other"
                    };
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Conventions")]
    public async Task IfChainOverAFrameworkEnumReportsNothing()
    {
        var test = """
            using System;

            namespace TestNamespace
            {
                public class Days
                {
                    public static string Name(DayOfWeek day)
                    {
                        if (day == DayOfWeek.Monday)
                            return "mon";
                        else if (day == DayOfWeek.Tuesday)
                            return "tue";
                        else if (day == DayOfWeek.Wednesday)
                            return "wed";
                        return "other";
                    }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Conventions")]
    public async Task SwitchStatementOverAnEnumDefinedInSourceReportsTheDeclarationAndTheSwitch()
    {
        var test = """
            namespace TestNamespace
            {
                public enum {|#0:Color|} { Red, Green }

                public class Painter
                {
                    public static string Name(Color color)
                    {
                        {|#1:switch|} (color)
                        {
                            case Color.Red: return "red";
                            default: return "other";
                        }
                    }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(
            test,
            VerifyCS.Diagnostic("FDW017").WithLocation(0).WithArguments("Color"),
            VerifyCS.Diagnostic("FDW018").WithLocation(1).WithArguments("Color"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Conventions")]
    public async Task SwitchExpressionOverAnEnumDefinedInSourceReportsTheDeclarationAndTheSwitch()
    {
        var test = """
            namespace TestNamespace
            {
                public enum {|#0:Color|} { Red, Green }

                public class Painter
                {
                    public static string Name(Color color) => color {|#1:switch|}
                    {
                        Color.Red => "red",
                        _ => "other"
                    };
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(
            test,
            VerifyCS.Diagnostic("FDW017").WithLocation(0).WithArguments("Color"),
            VerifyCS.Diagnostic("FDW018").WithLocation(1).WithArguments("Color"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Conventions")]
    public async Task IfChainOverAnEnumDefinedInSourceReportsTheDeclarationAndTheChain()
    {
        var test = """
            namespace TestNamespace
            {
                public enum {|#0:Color|} { Red, Green, Blue }

                public class Painter
                {
                    public static string Name(Color color)
                    {
                        {|#1:if|} (color == Color.Red)
                            return "red";
                        else if (color == Color.Green)
                            return "green";
                        else if (color == Color.Blue)
                            return "blue";
                        return "other";
                    }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(
            test,
            VerifyCS.Diagnostic("FDW017").WithLocation(0).WithArguments("Color"),
            VerifyCS.Diagnostic("FDW019").WithLocation(1).WithArguments(3, "color"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Conventions")]
    public async Task IfChainOverStringLiteralsIsStillReported()
    {
        var test = """
            namespace TestNamespace
            {
                public class Namer
                {
                    public static int Number(string name)
                    {
                        {|#0:if|} (name == "one")
                            return 1;
                        else if (name == "two")
                            return 2;
                        else if (name == "three")
                            return 3;
                        return 0;
                    }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(
            test,
            VerifyCS.Diagnostic("FDW019").WithLocation(0).WithArguments(3, "name"));
    }
}
