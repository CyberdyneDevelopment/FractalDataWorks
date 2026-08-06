using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Fdw.Analyzers.Tests;

/// <summary>
/// Tests for the FDW016 UncheckedResultValueAccess analyzer.
/// Verifies that accessing IGenericResult&lt;T&gt;.Value without an IsSuccess guard is flagged.
/// </summary>
public class UncheckedResultValueAccessAnalyzerTests : AnalyzerTestBase<UncheckedResultValueAccessAnalyzer>
{
    private const string GenericResultStubs = @"
using System;
using Fdw.Results;

namespace Fdw.Results
{
    public interface IGenericResult
    {
        bool IsSuccess { get; }
        bool IsFailure { get; }
        string CurrentMessage { get; }
    }

    public interface IGenericResult<T> : IGenericResult
    {
        T Value { get; }
    }

    public class GenericResult : IGenericResult
    {
        public bool IsSuccess { get; set; }
        public bool IsFailure => !IsSuccess;
        public string CurrentMessage { get; set; }

        public static IGenericResult Success() => new GenericResult { IsSuccess = true };
        public static IGenericResult Failure(string msg) => new GenericResult { IsSuccess = false };
    }

    public class GenericResult<T> : GenericResult, IGenericResult<T>
    {
        public T Value { get; set; }

        public new static IGenericResult<T> Success(T value) => new GenericResult<T> { IsSuccess = true, Value = value };
        public new static IGenericResult<T> Failure(string msg) => new GenericResult<T> { IsSuccess = false };
    }
}
";

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task EmptySourceNoDiagnostics()
    {
        await VerifyNoDiagnostics(string.Empty);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task ValueAccessAfterIsSuccessCheckNoDiagnostics()
    {
        var source = GenericResultStubs + @"
class Test
{
    string M()
    {
        var result = GenericResult<string>.Success(""hello"");
        if (result.IsSuccess)
        {
            return result.Value;
        }
        return string.Empty;
    }
}";
        await VerifyNoDiagnostics(source);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task ValueAccessAfterIsFailureGuardReturnNoDiagnostics()
    {
        var source = GenericResultStubs + @"
class Test
{
    string M()
    {
        var result = GenericResult<string>.Success(""hello"");
        if (result.IsFailure) return string.Empty;
        return result.Value;
    }
}";
        await VerifyNoDiagnostics(source);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task ValueAccessAfterNegatedIsSuccessGuardNoDiagnostics()
    {
        var source = GenericResultStubs + @"
class Test
{
    string M()
    {
        var result = GenericResult<string>.Success(""hello"");
        if (!result.IsSuccess) return string.Empty;
        return result.Value;
    }
}";
        await VerifyNoDiagnostics(source);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task ValueAccessWithoutAnyCheckDiagnostic()
    {
        var source = GenericResultStubs + @"
class Test
{
    string M()
    {
        var result = GenericResult<string>.Success(""hello"");
        return {|#0:result.Value|};
    }
}";

        var test = new CSharpAnalyzerTest<UncheckedResultValueAccessAnalyzer, DefaultVerifier>
        {
            TestCode = source,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult(UncheckedResultValueAccessAnalyzer.DiagnosticId, DiagnosticSeverity.Warning)
                .WithLocation(0));
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task ValueAccessInReturnWithoutCheckDiagnostic()
    {
        var source = GenericResultStubs + @"
class Test
{
    string M()
    {
        var result = GenericResult<string>.Success(""hello"");
        var x = 42;
        return {|#0:result.Value|};
    }
}";

        var test = new CSharpAnalyzerTest<UncheckedResultValueAccessAnalyzer, DefaultVerifier>
        {
            TestCode = source,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult(UncheckedResultValueAccessAnalyzer.DiagnosticId, DiagnosticSeverity.Warning)
                .WithLocation(0));
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task ValueAccessWithNullConditionalWithoutCheckDiagnostic()
    {
        var source = GenericResultStubs + @"
class Test
{
    int M()
    {
        var result = GenericResult<string>.Success(""hello"");
        return {|#0:result.Value|}?.Length ?? 0;
    }
}";

        var test = new CSharpAnalyzerTest<UncheckedResultValueAccessAnalyzer, DefaultVerifier>
        {
            TestCode = source,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult(UncheckedResultValueAccessAnalyzer.DiagnosticId, DiagnosticSeverity.Warning)
                .WithLocation(0));
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task MultipleResultsOnlyUncheckedOneReportsDiagnostic()
    {
        var source = GenericResultStubs + @"
class Test
{
    string M()
    {
        var checkedResult = GenericResult<string>.Success(""ok"");
        var uncheckedResult = GenericResult<string>.Success(""bad"");

        if (checkedResult.IsSuccess)
        {
            var val = checkedResult.Value;
        }

        return {|#0:uncheckedResult.Value|};
    }
}";

        var test = new CSharpAnalyzerTest<UncheckedResultValueAccessAnalyzer, DefaultVerifier>
        {
            TestCode = source,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult(UncheckedResultValueAccessAnalyzer.DiagnosticId, DiagnosticSeverity.Warning)
                .WithLocation(0));
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task PatternMatchingIsSuccessTrueNoDiagnostics()
    {
        var source = GenericResultStubs + @"
class Test
{
    string M()
    {
        var result = GenericResult<string>.Success(""hello"");
        if (result is { IsSuccess: true })
        {
            return result.Value;
        }
        return string.Empty;
    }
}";
        await VerifyNoDiagnostics(source);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task NonGenericResultNoDiagnostics()
    {
        var source = GenericResultStubs + @"
class Test
{
    string M()
    {
        var result = GenericResult.Success();
        if (result.IsSuccess)
        {
            return ""ok"";
        }
        return result.CurrentMessage;
    }
}";
        await VerifyNoDiagnostics(source);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task ValueAccessInShortCircuitAndNoDiagnostics()
    {
        var source = GenericResultStubs + @"
class Test
{
    bool M()
    {
        var result = GenericResult<string>.Success(""hello"");
        if (result.IsSuccess && result.Value != null)
        {
            return true;
        }
        return false;
    }
}";
        await VerifyNoDiagnostics(source);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task ValueAccessInTernaryGuardNoDiagnostics()
    {
        var source = GenericResultStubs + @"
class Test
{
    string M()
    {
        var result = GenericResult<string>.Success(""hello"");
        return result.IsSuccess ? result.Value : default;
    }
}";
        await VerifyNoDiagnostics(source);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task ValueAccessInElseOfIsFailureNoDiagnostics()
    {
        var source = GenericResultStubs + @"
class Test
{
    string M()
    {
        var result = GenericResult<string>.Success(""hello"");
        if (result.IsFailure)
        {
            return string.Empty;
        }
        else
        {
            return result.Value;
        }
    }
}";
        await VerifyNoDiagnostics(source);
    }
}
