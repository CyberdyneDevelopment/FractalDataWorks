using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Fdw.Analyzers.Tests;

/// <summary>
/// Tests for the FDW012 UncheckedGenericResult analyzer.
/// </summary>
public class UncheckedGenericResultAnalyzerTests : AnalyzerTestBase<UncheckedGenericResultAnalyzer>
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
        public new static IGenericResult<T> Failure(params object[] msgs) => new GenericResult<T> { IsSuccess = false };
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
    public async Task NonResultMethodCall_NoDiagnostics()
    {
        var source = @"
class Test
{
    void M()
    {
        System.Console.WriteLine(""hello"");
    }
}";
        await VerifyNoDiagnostics(source);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task ResultCheckedViaIsSuccess_NoDiagnostics()
    {
        var source = GenericResultStubs + @"
class Test
{
    void M()
    {
        var svc = new Service();
        var result = svc.Execute();
        if (result.IsSuccess) { }
    }
}

class Service
{
    public IGenericResult Execute() => GenericResult.Success();
}
";
        await VerifyNoDiagnostics(source);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task ResultReturnedDirectly_NoDiagnostics()
    {
        var source = GenericResultStubs + @"
class Service
{
    public IGenericResult Execute() => GenericResult.Success();
}

class Test
{
    IGenericResult M()
    {
        var svc = new Service();
        return svc.Execute();
    }
}
";
        await VerifyNoDiagnostics(source);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task ResultPassedAsArgument_NoDiagnostics()
    {
        var source = GenericResultStubs + @"
class Service
{
    public IGenericResult Execute() => GenericResult.Success();
}

class Test
{
    void Handle(IGenericResult r) { }

    void M()
    {
        var svc = new Service();
        var result = svc.Execute();
        Handle(result);
    }
}
";
        await VerifyNoDiagnostics(source);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task ResultCheckedViaIsFailure_NoDiagnostics()
    {
        var source = GenericResultStubs + @"
class Service
{
    public IGenericResult Execute() => GenericResult.Success();
}

class Test
{
    void M()
    {
        var svc = new Service();
        var result = svc.Execute();
        if (result.IsFailure) { return; }
    }
}
";
        await VerifyNoDiagnostics(source);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task ResultCheckedViaMessages_NoDiagnostics()
    {
        var source = GenericResultStubs + @"
class Service
{
    public IGenericResult Execute() => GenericResult.Success();
}

class Test
{
    void M()
    {
        var svc = new Service();
        var result = svc.Execute();
        var msgs = result.Messages;
    }
}
";
        await VerifyNoDiagnostics(source);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task FireAndForgetExpressionStatement_Diagnostic()
    {
        var source = GenericResultStubs + @"
class Service
{
    public IGenericResult Execute() => GenericResult.Success();
}

class Test
{
    void M()
    {
        var svc = new Service();
        {|#0:svc.Execute();|}
    }
}
";

        var test = new Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerTest<UncheckedGenericResultAnalyzer, DefaultVerifier>
        {
            TestCode = source,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };

        test.ExpectedDiagnostics.Add(
            new DiagnosticResult(UncheckedGenericResultAnalyzer.DiagnosticId, DiagnosticSeverity.Warning)
                .WithLocation(0));

        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task AssignedButNeverChecked_Diagnostic()
    {
        var source = GenericResultStubs + @"
class Service
{
    public IGenericResult Execute() => GenericResult.Success();
}

class Test
{
    void M()
    {
        var svc = new Service();
        {|#0:var result = svc.Execute();|}
        System.Console.WriteLine(""done"");
    }
}
";

        var test = new Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerTest<UncheckedGenericResultAnalyzer, DefaultVerifier>
        {
            TestCode = source,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };

        test.ExpectedDiagnostics.Add(
            new DiagnosticResult(UncheckedGenericResultAnalyzer.DiagnosticId, DiagnosticSeverity.Warning)
                .WithLocation(0));

        await test.RunAsync(TestContext.Current.CancellationToken);
    }
}
