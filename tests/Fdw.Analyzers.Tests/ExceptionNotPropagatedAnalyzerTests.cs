using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Fdw.Analyzers.Tests;

/// <summary>
/// Tests for the FDW014 ExceptionNotPropagated analyzer.
/// </summary>
public class ExceptionNotPropagatedAnalyzerTests : AnalyzerTestBase<ExceptionNotPropagatedAnalyzer>
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
    }

    public interface IGenericResult<T> : IGenericResult
    {
        T Value { get; }
    }

    public class GenericResult : IGenericResult
    {
        public bool IsSuccess { get; set; }
        public bool IsFailure => !IsSuccess;

        public static IGenericResult Success() => new GenericResult { IsSuccess = true };
        public static IGenericResult Failure(string msg) => new GenericResult { IsSuccess = false };
        public static IGenericResult Failure(object msg) => new GenericResult { IsSuccess = false };
        public static IGenericResult Chain(object code, IGenericResult inner) => inner;
    }

    public class GenericResult<T> : GenericResult, IGenericResult<T>
    {
        public T Value { get; set; }

        public new static IGenericResult<T> Success(T value) => new GenericResult<T> { IsSuccess = true, Value = value };
        public static IGenericResult<T> Success(T value, string message) => new GenericResult<T> { IsSuccess = true, Value = value };
        public new static IGenericResult<T> Failure(string msg) => new GenericResult<T> { IsSuccess = false };
        public new static IGenericResult<T> Failure(object msg) => new GenericResult<T> { IsSuccess = false };
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
    public async Task CatchReturnsFailureWithMessage_NoDiagnostics()
    {
        var source = GenericResultStubs + @"
class Test
{
    IGenericResult M()
    {
        try
        {
            return GenericResult.Success();
        }
        catch (Exception ex)
        {
            return GenericResult.Failure(ex.Message);
        }
    }
}";
        await VerifyNoDiagnostics(source);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task CatchRethrows_NoDiagnostics()
    {
        var source = GenericResultStubs + @"
class Test
{
    IGenericResult M()
    {
        try
        {
            return GenericResult.Success();
        }
        catch (Exception ex)
        {
            System.Console.WriteLine(ex.Message);
            throw;
        }
    }
}";
        await VerifyNoDiagnostics(source);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task CatchReturnsChain_NoDiagnostics()
    {
        var source = GenericResultStubs + @"
class Test
{
    IGenericResult M()
    {
        try
        {
            return GenericResult.Success();
        }
        catch (Exception ex)
        {
            return GenericResult.Chain(null, GenericResult.Failure(ex.Message));
        }
    }
}";
        await VerifyNoDiagnostics(source);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task CatchReturnsSuccessWithMessage_NoDiagnostics()
    {
        var source = GenericResultStubs + @"
class Test
{
    IGenericResult<int> M()
    {
        try
        {
            return GenericResult<int>.Success(42);
        }
        catch (Exception ex)
        {
            return GenericResult<int>.Success(0, ex.Message);
        }
    }
}";
        await VerifyNoDiagnostics(source);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task NonResultReturningMethod_NoDiagnostics()
    {
        var source = @"
using System;

class Test
{
    void M()
    {
        try
        {
            System.Console.WriteLine(""ok"");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine(ex.Message);
        }
    }
}";
        await VerifyNoDiagnostics(source);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task CatchWithNoReturn_Diagnostic()
    {
        var source = GenericResultStubs + @"
class Test
{
    IGenericResult M()
    {
        try
        {
            return GenericResult.Success();
        }
        {|#0:catch|} (Exception ex)
        {
            System.Console.WriteLine(ex.Message);
        }
        return GenericResult.Success();
    }
}";

        var test = new Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerTest<ExceptionNotPropagatedAnalyzer, DefaultVerifier>
        {
            TestCode = source,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };

        test.ExpectedDiagnostics.Add(
            new DiagnosticResult(ExceptionNotPropagatedAnalyzer.DiagnosticId, DiagnosticSeverity.Warning)
                .WithLocation(0));

        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task CatchReturnsSuccessWithoutMessage_Diagnostic()
    {
        var source = GenericResultStubs + @"
class Test
{
    IGenericResult<int> M()
    {
        try
        {
            return GenericResult<int>.Success(42);
        }
        catch (Exception ex)
        {
            System.Console.WriteLine(ex.Message);
            {|#0:return GenericResult<int>.Success(0);|}
        }
    }
}";

        var test = new Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerTest<ExceptionNotPropagatedAnalyzer, DefaultVerifier>
        {
            TestCode = source,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };

        test.ExpectedDiagnostics.Add(
            new DiagnosticResult(ExceptionNotPropagatedAnalyzer.DiagnosticId, DiagnosticSeverity.Warning)
                .WithLocation(0));

        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task CatchReturnsBareSuccess_Diagnostic()
    {
        var source = GenericResultStubs + @"
class Test
{
    IGenericResult M()
    {
        try
        {
            return GenericResult.Success();
        }
        catch (Exception ex)
        {
            System.Console.WriteLine(ex.Message);
            {|#0:return GenericResult.Success();|}
        }
    }
}";

        var test = new Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerTest<ExceptionNotPropagatedAnalyzer, DefaultVerifier>
        {
            TestCode = source,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };

        test.ExpectedDiagnostics.Add(
            new DiagnosticResult(ExceptionNotPropagatedAnalyzer.DiagnosticId, DiagnosticSeverity.Warning)
                .WithLocation(0));

        await test.RunAsync(TestContext.Current.CancellationToken);
    }
}
