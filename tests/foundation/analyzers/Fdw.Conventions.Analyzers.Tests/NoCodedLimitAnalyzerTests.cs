using System.Threading.Tasks;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    Fdw.Conventions.Analyzers.NoCodedLimitAnalyzer,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Fdw.Conventions.Analyzers.Tests;

/// <summary>
/// FDW073 (FDW-7059): a limit is configuration, never code (Mike, 2026-11-04). A public int or long constant named Max, Maximum, Default or Limit reports,
/// unless it is a declared default ([PlatformDefault]), a protocol or format fact ([FormatLimit("reason")]) or a cap with a filed story
/// ([ConfigurationPending("FDW-n")]).
/// </summary>
public class NoCodedLimitAnalyzerTests
{
    private const string Support = """
        namespace Fdw.Conventions
        {
            [System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property)]
            public sealed class PlatformDefaultAttribute : System.Attribute { }

            [System.AttributeUsage(System.AttributeTargets.Field)]
            public sealed class FormatLimitAttribute : System.Attribute
            {
                public FormatLimitAttribute(string reason) { Reason = reason; }
                public string Reason { get; }
            }

            [System.AttributeUsage(System.AttributeTargets.Field)]
            public sealed class ConfigurationPendingAttribute : System.Attribute
            {
                public ConfigurationPendingAttribute(string item) { Item = item; }
                public string Item { get; }
            }
        }

        """;

    private static string Coded(string name, string value) =>
        $"'{name}' is a limit written in code ({value}). A limit is configuration: declare its default in the service type option's DeclaredDefaults, read it at Register with DeclaredSetting.ConfiguredCount (a host changes it with ServiceTypeDefaults.Override, and a value the host's configuration states wins), and hand the configured value to what checks it (see DefaultMessagingServiceType). If a protocol or file format fixes the number, mark it [FormatLimit(\"which format, and why\")]";

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Analyzer")]
    public async Task ALimitWrittenInCodeReportsWhateverItsPrefix()
    {
        var test = Support + """
            public static class Caps
            {
                public const int {|#0:MaxRows|} = 20000;
                public const long {|#1:MaximumSeconds|} = 2592000;
                public const int {|#2:DefaultTake|} = 50;
                public const int {|#3:LimitPerCall|} = 100;
                public const int {|#4:Max|} = 8;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(
            test,
            VerifyCS.Diagnostic("FDW073").WithLocation(0).WithArguments(Coded("MaxRows", "20000")),
            VerifyCS.Diagnostic("FDW073").WithLocation(1).WithArguments(Coded("MaximumSeconds", "2592000")),
            VerifyCS.Diagnostic("FDW073").WithLocation(2).WithArguments(Coded("DefaultTake", "50")),
            VerifyCS.Diagnostic("FDW073").WithLocation(3).WithArguments(Coded("LimitPerCall", "100")),
            VerifyCS.Diagnostic("FDW073").WithLocation(4).WithArguments(Coded("Max", "8")));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Analyzer")]
    public async Task ADeclaredDefaultAFormatFactAndAPendingCapAreSilent()
    {
        var test = Support + """
            public static class Caps
            {
                [Fdw.Conventions.PlatformDefault]
                public const int DefaultMaxRows = 100000;

                [Fdw.Conventions.FormatLimit("The Excel file format: a worksheet holds 1,048,576 rows")]
                public const int MaxRowsPerSheet = 1048576;

                [Fdw.Conventions.ConfigurationPending("FDW-7103")]
                public const int MaxAssignments = 1000;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Analyzer")]
    public async Task AMarkWithNoReasonOrNoItemKeyReports()
    {
        var test = Support + """
            public static class Caps
            {
                [Fdw.Conventions.FormatLimit(" ")]
                public const int {|#0:MaxNameLength|} = 128;

                [Fdw.Conventions.ConfigurationPending("later")]
                public const int {|#1:MaxDepth|} = 5;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(
            test,
            VerifyCS.Diagnostic("FDW073").WithLocation(0)
                .WithArguments("'MaxNameLength' is marked [FormatLimit] with no reason: name the protocol or file format that fixes 128 and how it fixes it"),
            VerifyCS.Diagnostic("FDW073").WithLocation(1)
                .WithArguments("'MaxDepth' is marked [ConfigurationPending] with 'later', which is not an item key: name the filed story that makes it configuration, such as FDW-7103"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Analyzer")]
    public async Task WhatIsNotAPublicWholeNumberLimitConstantIsSilent()
    {
        var test = Support + """
            public static class Other
            {
                private const int MaxPrivate = 10;
                internal const int MaxInternal = 10;
                public const string DefaultName = "x";
                public const double MaxRatio = 0.5;
                public const int Maxwell = 3;
                public const int Defaults = 3;
                public const int PageSize = 50;
                public static int MaxSettable { get; set; } = 200;
                public static readonly int MaxReadOnly = 200;
                public static int Read() => MaxPrivate + MaxInternal;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }
}
