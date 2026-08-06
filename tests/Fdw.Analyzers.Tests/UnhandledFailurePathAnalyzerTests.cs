using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Fdw.Analyzers.Tests;

/// <summary>
/// Tests for the FDW013 UnhandledFailurePath analyzer.
/// </summary>
public class UnhandledFailurePathAnalyzerTests : AnalyzerTestBase<UnhandledFailurePathAnalyzer>
{
    private const string GenericResultStubs = @"
using Fdw.Results;

namespace Fdw.Results
{
    public interface IGenericResult
    {
        bool IsSuccess { get; }
        bool IsFailure { get; }
        string CurrentMessage { get; }
        System.Collections.Generic.IReadOnlyList<object> Messages { get; }
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
        public System.Collections.Generic.IReadOnlyList<object> Messages { get; set; }

        public static IGenericResult Success() => new GenericResult { IsSuccess = true };
        public static IGenericResult Failure(string msg) => new GenericResult { IsSuccess = false };
        public static IGenericResult Failure(params object[] msgs) => new GenericResult { IsSuccess = false };
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
    public async Task EmptySource_NoDiagnostics()
    {
        await VerifyNoDiagnostics(string.Empty);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task IfWithElse_NoDiagnostics()
    {
        var source = GenericResultStubs + @"
class Test
{
    IGenericResult M()
    {
        var result = GenericResult.Success();
        if (result.IsSuccess)
        {
            System.Console.WriteLine(""ok"");
        }
        else
        {
            return GenericResult.Failure(""error"");
        }
        return GenericResult.Success();
    }
}";
        await VerifyNoDiagnostics(source);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task GuardPattern_NoDiagnostics()
    {
        var source = GenericResultStubs + @"
class Test
{
    IGenericResult M()
    {
        var result = GenericResult.Success();
        if (result.IsFailure)
        {
            return GenericResult.Failure(""error"");
        }
        return GenericResult.Success();
    }
}";
        await VerifyNoDiagnostics(source);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task NegatedGuardPattern_NoDiagnostics()
    {
        var source = GenericResultStubs + @"
class Test
{
    IGenericResult M()
    {
        var result = GenericResult.Success();
        if (!result.IsSuccess)
        {
            return GenericResult.Failure(""error"");
        }
        return GenericResult.Success();
    }
}";
        await VerifyNoDiagnostics(source);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task IfBodyAlwaysReturns_NoDiagnostics()
    {
        var source = GenericResultStubs + @"
class Test
{
    IGenericResult M()
    {
        var result = GenericResult.Success();
        if (result.IsSuccess)
        {
            return GenericResult.Success();
        }
        return GenericResult.Failure(""error"");
    }
}";
        await VerifyNoDiagnostics(source);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task SubsequentFailureCheck_NoDiagnostics()
    {
        var source = GenericResultStubs + @"
class Test
{
    IGenericResult M()
    {
        var result = GenericResult.Success();
        if (result.IsSuccess)
        {
            System.Console.WriteLine(""ok"");
        }
        if (result.IsFailure)
        {
            return GenericResult.Failure(""error"");
        }
        return GenericResult.Success();
    }
}";
        await VerifyNoDiagnostics(source);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task SuccessCheckWithNoElse_Diagnostic()
    {
        var source = GenericResultStubs + @"
class Test
{
    IGenericResult M()
    {
        var result = GenericResult.Success();
        {|#0:if|} (result.IsSuccess)
        {
            System.Console.WriteLine(""ok"");
        }
        return GenericResult.Success();
    }
}";

        var test = new Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerTest<UnhandledFailurePathAnalyzer, DefaultVerifier>
        {
            TestCode = source,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };

        test.ExpectedDiagnostics.Add(
            new DiagnosticResult(UnhandledFailurePathAnalyzer.DiagnosticId, DiagnosticSeverity.Warning)
                .WithLocation(0));

        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task CompoundConditionWithNoElse_Diagnostic()
    {
        var source = GenericResultStubs + @"
class Test
{
    IGenericResult<string> M()
    {
        var result = GenericResult<string>.Success(""value"");
        {|#0:if|} (result.IsSuccess && result.Value != null)
        {
            System.Console.WriteLine(result.Value);
        }
        return GenericResult<string>.Success(""done"");
    }
}";

        var test = new Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerTest<UnhandledFailurePathAnalyzer, DefaultVerifier>
        {
            TestCode = source,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };

        test.ExpectedDiagnostics.Add(
            new DiagnosticResult(UnhandledFailurePathAnalyzer.DiagnosticId, DiagnosticSeverity.Warning)
                .WithLocation(0));

        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task NonResultIfStatement_NoDiagnostics()
    {
        var source = @"
class Test
{
    void M()
    {
        var x = true;
        if (x)
        {
            System.Console.WriteLine(""ok"");
        }
    }
}";
        await VerifyNoDiagnostics(source);
    }
}
