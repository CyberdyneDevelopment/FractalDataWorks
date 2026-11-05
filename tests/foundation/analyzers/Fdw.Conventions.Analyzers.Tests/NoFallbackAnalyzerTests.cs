using System.Threading.Tasks;
using Fdw.Conventions.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    Fdw.Conventions.Analyzers.NoFallbackAnalyzer,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Fdw.Conventions.Analyzers.Tests;

/// <summary>
/// FDW055 (FDW-3580): a <c>??</c> or <c>??=</c> never falls back to a value that could be a real one, as Mike ruled on
/// 2026-11-04. The allowed forms are silent, every other fallback reports, and <c>""</c> and the long spellings of an
/// empty collection are fixed to <c>string.Empty</c> and <c>[]</c>.
/// </summary>
public class NoFallbackAnalyzerTests
{
    private const string FailLoud = "fail loud with a MessageLogging message and a failed result";

    private const string Support = """
        namespace Fdw.Conventions
        {
            [System.AttributeUsage(System.AttributeTargets.Method | System.AttributeTargets.Constructor | System.AttributeTargets.Property | System.AttributeTargets.Field)]
            public sealed class ApprovedFallbackAttribute : System.Attribute
            {
                public ApprovedFallbackAttribute(string reason) { Reason = reason; }
                public string Reason { get; }
            }

            [System.AttributeUsage(System.AttributeTargets.Class)]
            public sealed class PlaceholderTextAttribute : System.Attribute { }

            [System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property)]
            public sealed class PlatformDefaultAttribute : System.Attribute { }
        }

        namespace Fdw.Results
        {
            public interface IGenericResult { string? CurrentMessage { get; } }
        }

        namespace Fdw.Messages
        {
            public interface IGenericMessage { }
        }

        namespace Logging
        {
            public interface ILogger<T> { }
            public sealed class NullLogger<T> : ILogger<T> { public static NullLogger<T> Instance { get; } = new(); }
            public interface ILoggerFactory { }
            public sealed class NullLoggerFactory : ILoggerFactory { public static NullLoggerFactory Instance { get; } = new(); }
        }

        [Fdw.Conventions.PlaceholderText]
        public static class Placeholders
        {
            public const string None = "(none)";
        }

        public static class Limits
        {
            [Fdw.Conventions.PlatformDefault]
            public const int DefaultTake = 50;

            public const int Take = 50;
        }

        public sealed class Options { }

        """;

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Analyzer")]
    public async Task TheAllowedFormsAreSilent()
    {
        var test = Support + """
            public class Sample
            {
                private readonly Logging.ILogger<Sample> _logger;
                private Logging.ILoggerFactory? _factory;
                private string? _derived;

                public Sample(Logging.ILogger<Sample>? logger)
                {
                    _logger = logger ?? Logging.NullLogger<Sample>.Instance;
                }

                public Logging.ILoggerFactory Factory() => _factory ??= Logging.NullLoggerFactory.Instance;

                public Logging.ILogger<Sample> Cast(Logging.ILogger<Sample>? logger) => logger ?? (Logging.ILogger<Sample>)Logging.NullLogger<Sample>.Instance;

                public string Name(string? name) => name ?? throw new System.ArgumentNullException(nameof(name));

                public string Text(string? text) => text ?? string.Empty;

                public System.Collections.Generic.IReadOnlyList<string> Items(System.Collections.Generic.IReadOnlyList<string>? items) => items ?? [];

                public string Derived() => _derived ??= Name("x");

                public Fdw.Results.IGenericResult? FirstFailure(Fdw.Results.IGenericResult? a, Fdw.Results.IGenericResult? b) => a ?? b;

                public string Shown(string? value) => value ?? Placeholders.None;

                public int Take(int? take) => take ?? Limits.DefaultTake;

                public string KeepFirst(string[] values)
                {
                    string? first = null;
                    foreach (var value in values)
                        first ??= value;
                    return first ?? string.Empty;
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Analyzer")]
    public async Task ValuesThatCouldBeRealReport()
    {
        var test = Support + """
            public class Sample
            {
                public string? Label { get; set; }
                public string Id { get; set; } = string.Empty;

                public string Name(string? name) => {|#0:name ?? "Unknown"|};

                public int Timeout(int? seconds) => {|#1:seconds ?? 30|};

                public bool Enabled(bool? enabled) => {|#2:enabled ?? false|};

                public Options Settings(Options? options) => {|#3:options ?? new Options()|};

                public string Display() => {|#4:Label ?? Id|};

                public int Take(int? take) => {|#5:take ?? Limits.Take|};
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(
            test,
            VerifyCS.Diagnostic("FDW055").WithLocation(0).WithArguments(BareLiteral("\"Unknown\"")),
            VerifyCS.Diagnostic("FDW055").WithLocation(1).WithArguments(Fallback("30")),
            VerifyCS.Diagnostic("FDW055").WithLocation(2).WithArguments(Fallback("false")),
            VerifyCS.Diagnostic("FDW055").WithLocation(3).WithArguments(Fallback("new Options()")),
            VerifyCS.Diagnostic("FDW055").WithLocation(4).WithArguments(Fallback("Id")),
            VerifyCS.Diagnostic("FDW055").WithLocation(5).WithArguments(Fallback("Limits.Take")));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Analyzer")]
    public async Task TheSameFieldOfAParentOrStoredRecordIsSilentAndAnotherFieldReports()
    {
        var test = Support + """
            public sealed class Policy
            {
                public int? MaxParallel { get; set; }
                public string? Description { get; set; }
                public string? Name { get; set; }
            }

            public sealed class Effective
            {
                public int MaxParallel { get; set; }
            }

            public sealed class Stored
            {
                public string Description { get; set; } = string.Empty;
                public string Name { get; set; } = string.Empty;
            }

            public class Sample
            {
                public int Inherit(Policy child, Effective parent) => child.MaxParallel ?? parent.MaxParallel;

                public string Keep(Policy request, Stored existing) => request.Description ?? existing.Description;

                public string Substitute(Policy request, Stored existing) => {|#0:request.Name ?? existing.Description|};

                public string? SameRecord(Policy request) => {|#1:request.Description ?? request.Name|};
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(
            test,
            VerifyCS.Diagnostic("FDW055").WithLocation(0).WithArguments(Fallback("existing.Description")),
            VerifyCS.Diagnostic("FDW055").WithLocation(1).WithArguments(Fallback("request.Name")));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Analyzer")]
    public async Task SqlNullTheNotFoundSentinelAndTheEmptyDictionaryAreSilentAndANewDictionaryReports()
    {
        var test = Support + """
            public sealed class Converter
            {
                public static Converter NotFound { get; } = new();
            }

            public class Sample
            {
                public object Parameter(object? value) => value ?? System.DBNull.Value;

                public Converter Lookup(Converter? found) => found ?? Converter.NotFound;

                public System.Collections.Generic.IReadOnlyDictionary<string, string> Headers(System.Collections.Generic.IReadOnlyDictionary<string, string>? headers) =>
                    headers ?? System.Collections.Immutable.ImmutableDictionary<string, string>.Empty;

                public System.Collections.Generic.IReadOnlyDictionary<string, string> NewHeaders(System.Collections.Generic.IReadOnlyDictionary<string, string>? headers) =>
                    {|#0:headers ?? new System.Collections.Generic.Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)|};
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(
            test,
            VerifyCS.Diagnostic("FDW055").WithLocation(0).WithArguments(
                "'new System.Collections.Generic.Dictionary<string, string>(Sy...' is the empty dictionary: write ImmutableDictionary<K, V>.Empty, since [] does not convert to a dictionary interface"));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Analyzer")]
    public async Task AValidValueInitializerOnAStatedConfigurationReportsAndAPlatformDefaultIsSilent()
    {
        var test = Support + """
            public interface IConnectionImplementationConfiguration { }

            public class SqlConnectionSettings : IConnectionImplementationConfiguration
            {
                public int CommandTimeoutSeconds { get; set; } {|#0:= 30|};
                public bool Encrypt { get; set; } {|#1:= true|};
                public string Server { get; set; } {|#2:= "localhost"|};
                public int Take { get; set; } {|#3:= Limits.Take|};
                public int DefaultTake { get; set; } = Limits.DefaultTake;
                public string Database { get; set; } = string.Empty;
                public bool Pooling { get; set; } = false;
                public int Retries { get; set; } = 0;
            }

            public class Unrelated
            {
                public int Seconds { get; set; } = 30;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(
            test,
            VerifyCS.Diagnostic("FDW055").WithLocation(0).WithArguments(Assumed("30", "CommandTimeoutSeconds")),
            VerifyCS.Diagnostic("FDW055").WithLocation(1).WithArguments(Assumed("true", "Encrypt")),
            VerifyCS.Diagnostic("FDW055").WithLocation(2).WithArguments(Assumed("\"localhost\"", "Server")),
            VerifyCS.Diagnostic("FDW055").WithLocation(3).WithArguments(Assumed("Limits.Take", "Take")));
    }

    private static string Assumed(string value, string property) =>
        $"'{value}' is assumed for '{property}': a configuration row states it, so remove the initializer and {FailLoud} where it is missing; a driver's documented default is a [PlatformDefault] member";

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Analyzer")]
    public async Task ACompilerExtensionIsNotAnalyzed()
    {
        var test = new CSharpAnalyzerTest<NoFallbackAnalyzer, DefaultVerifier>
        {
            TestCode = Support + """
                [Microsoft.CodeAnalysis.Generator]
                public sealed class SampleGenerator : Microsoft.CodeAnalysis.IIncrementalGenerator
                {
                    public void Initialize(Microsoft.CodeAnalysis.IncrementalGeneratorInitializationContext context) { }

                    public string Name(string? name) => name ?? "Unknown";
                }
                """,
        };
        test.TestState.AdditionalReferences.Add(typeof(Microsoft.CodeAnalysis.Compilation).Assembly);

        // Why: the Roslyn this test loads targets a newer runtime than the test's reference assemblies (CS1705); only the analyzer's
        // diagnostics are under test.
        test.CompilerDiagnostics = CompilerDiagnostics.None;

        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Analyzer")]
    public async Task ADefaultMessageReportsEvenAsAPlaceholder()
    {
        var test = Support + """
            public class Sample
            {
                public string Reason(Fdw.Results.IGenericResult result) => {|#0:result.CurrentMessage ?? Placeholders.None|};

                public string Literal(Fdw.Results.IGenericResult result) => {|#1:result.CurrentMessage ?? "Unknown error"|};
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(
            test,
            VerifyCS.Diagnostic("FDW055").WithLocation(0).WithArguments(DefaultMessage("Placeholders.None")),
            VerifyCS.Diagnostic("FDW055").WithLocation(1).WithArguments(DefaultMessage("\"Unknown error\"")));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Analyzer")]
    public async Task CoalesceAssignmentOnAParameterReports()
    {
        var test = Support + """
            public class Sample
            {
                public Options Run(Options? options)
                {
                    {|#0:options ??= new Options()|};
                    return options;
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(
            test,
            VerifyCS.Diagnostic("FDW055").WithLocation(0).WithArguments($"'??=' on parameter 'options' defaults an argument: {FailLoud}"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Analyzer")]
    public async Task AnApprovedFallbackIsSilentAndAnEmptyReasonReports()
    {
        var test = Support + """
            public class Sample
            {
                [Fdw.Conventions.ApprovedFallback("Mike 2026-11-04: a stage inherits its project's setting")]
                public bool AllowResume(bool? stage, bool project)
                {
                    return Effective();

                    bool Effective() => stage ?? project;
                }

                [Fdw.Conventions.ApprovedFallback("")]
                public string Label(string? label, string id) => {|#0:label ?? id|};
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(
            test,
            VerifyCS.Diagnostic("FDW055").WithLocation(0).WithArguments("[ApprovedFallback] needs the reason Mike gave for this fallback"));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CodeFix")]
    public async Task AnEmptyLiteralIsFixedToStringEmpty()
    {
        await VerifyFix(
            Support + """
                public class Sample
                {
                    public string Text(string? text) => {|#0:text ?? ""|};
                }
                """,
            Support + """
                public class Sample
                {
                    public string Text(string? text) => text ?? string.Empty;
                }
                """,
            new DiagnosticResult("FDW055", Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
                .WithLocation(0)
                .WithArguments("'\"\"' is written string.Empty"));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CodeFix")]
    public async Task AnEmptyCollectionSpelledLongIsFixedToBrackets()
    {
        await VerifyFix(
            Support + """
                public class Sample
                {
                    public System.Collections.Generic.List<string> Names(System.Collections.Generic.List<string>? names) => {|#0:names ?? new System.Collections.Generic.List<string>()|};

                    public string[] Tags(string[]? tags) => {|#1:tags ?? System.Array.Empty<string>()|};
                }
                """,
            Support + """
                public class Sample
                {
                    public System.Collections.Generic.List<string> Names(System.Collections.Generic.List<string>? names) => names ?? [];

                    public string[] Tags(string[]? tags) => tags ?? [];
                }
                """,
            new DiagnosticResult("FDW055", Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
                .WithLocation(0)
                .WithArguments(EmptyCollection("new System.Collections.Generic.List<string>()")),
            new DiagnosticResult("FDW055", Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
                .WithLocation(1)
                .WithArguments(EmptyCollection("System.Array.Empty<string>()")));
    }

    private static string Fallback(string supplied) =>
        $"'{supplied}' is a fallback value: {FailLoud}, or have Mike approve it with [ApprovedFallback(\"reason\")]";

    private static string BareLiteral(string supplied) =>
        $"'{supplied}' is a bare literal: placeholder text is a const in a [PlaceholderText] class, and a real value must {FailLoud}";

    private static string DefaultMessage(string supplied) =>
        $"'{supplied}' is a default message: give the thing you consume a message, or make a new one, instead of a fallback";

    private static string EmptyCollection(string supplied) =>
        $"'{supplied}' is the empty collection: write []";

    private static async Task VerifyFix(string source, string fixedSource, params DiagnosticResult[] expected)
    {
        var test = new CSharpCodeFixTest<NoFallbackAnalyzer, NoFallbackCodeFixProvider, DefaultVerifier>
        {
            TestCode = source,
            FixedCode = fixedSource,
        };

        test.ExpectedDiagnostics.AddRange(expected);
        await test.RunAsync();
    }
}
