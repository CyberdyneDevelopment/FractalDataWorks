using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Fdw.Analyzers.Tests;

/// <summary>
/// Tests for the FDW015 BrokenResultChain analyzer.
/// </summary>
public class BrokenResultChainAnalyzerTests : AnalyzerTestBase<BrokenResultChainAnalyzer>
{
    private const string GenericResultStubs = @"
using Fdw.Results;
using Fdw.Messages;

namespace Fdw.Messages
{
    public interface IGenericMessage
    {
        string Message { get; }
    }

    public class GenericMessage : IGenericMessage
    {
        public string Message { get; set; }
        public GenericMessage(string msg) { Message = msg; }
    }
}

namespace Fdw.Results
{
    public interface IGenericResult
    {
        bool IsSuccess { get; }
        bool IsFailure { get; }
        string CurrentMessage { get; }
        System.Collections.Generic.IReadOnlyList<Fdw.Messages.IGenericMessage> Messages { get; }
        object Code { get; }
        object Details { get; }
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
        public System.Collections.Generic.IReadOnlyList<Fdw.Messages.IGenericMessage> Messages { get; set; }
        public object Code { get; set; }
        public object Details { get; set; }

        public static IGenericResult Success() => new GenericResult { IsSuccess = true };
        public static IGenericResult Failure(string msg) => new GenericResult { IsSuccess = false };
        public static IGenericResult Failure(Fdw.Messages.IGenericMessage msg) => new GenericResult { IsSuccess = false };
        public static IGenericResult Failure(params Fdw.Messages.IGenericMessage[] msgs) => new GenericResult { IsSuccess = false };
        public static IGenericResult Failure(System.Collections.Generic.IReadOnlyList<Fdw.Messages.IGenericMessage> msgs) => new GenericResult { IsSuccess = false };
        public static IGenericResult Failure(string msg, object details) => new GenericResult { IsSuccess = false };
        public static IGenericResult Chain(object code, IGenericResult inner) => inner;
    }

    public class GenericResult<T> : GenericResult, IGenericResult<T>
    {
        public T Value { get; set; }

        public new static IGenericResult<T> Success(T value) => new GenericResult<T> { IsSuccess = true, Value = value };
        public new static IGenericResult<T> Failure(string msg) => new GenericResult<T> { IsSuccess = false };
        public new static IGenericResult<T> Failure(Fdw.Messages.IGenericMessage msg) => new GenericResult<T> { IsSuccess = false };
        public new static IGenericResult<T> Failure(params Fdw.Messages.IGenericMessage[] msgs) => new GenericResult<T> { IsSuccess = false };
        public new static IGenericResult<T> Chain(object code, IGenericResult inner) => new GenericResult<T> { IsSuccess = false };
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
    public async Task FailureWithNewMessage_NoDiagnostics()
    {
        var source = GenericResultStubs + @"
class Test
{
    IGenericResult M()
    {
        var msg = new GenericMessage(""error"");
        return GenericResult.Failure(msg);
    }
}";
        await VerifyNoDiagnostics(source);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task ChainCall_NoDiagnostics()
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
        var innerResult = svc.Execute();
        if (innerResult.IsFailure)
        {
            return GenericResult.Chain(null, innerResult);
        }
        return GenericResult.Success();
    }
}";
        await VerifyNoDiagnostics(source);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task FailureWithStringMessage_NoDiagnostics()
    {
        var source = GenericResultStubs + @"
class Test
{
    IGenericResult M()
    {
        return GenericResult.Failure(""something went wrong"");
    }
}";
        await VerifyNoDiagnostics(source);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task FailureWithResultMessages_Diagnostic()
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
        var innerResult = svc.Execute();
        if (innerResult.IsFailure)
        {
            return {|#0:GenericResult.Failure(innerResult.Messages)|};
        }
        return GenericResult.Success();
    }
}";

        var test = new Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerTest<BrokenResultChainAnalyzer, DefaultVerifier>
        {
            TestCode = source,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };

        test.ExpectedDiagnostics.Add(
            new DiagnosticResult(BrokenResultChainAnalyzer.DiagnosticId, DiagnosticSeverity.Warning)
                .WithLocation(0));

        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task FailureWithResultCurrentMessage_Diagnostic()
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
        var innerResult = svc.Execute();
        if (innerResult.IsFailure)
        {
            return {|#0:GenericResult.Failure(innerResult.CurrentMessage)|};
        }
        return GenericResult.Success();
    }
}";

        var test = new Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerTest<BrokenResultChainAnalyzer, DefaultVerifier>
        {
            TestCode = source,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };

        test.ExpectedDiagnostics.Add(
            new DiagnosticResult(BrokenResultChainAnalyzer.DiagnosticId, DiagnosticSeverity.Warning)
                .WithLocation(0));

        await test.RunAsync(TestContext.Current.CancellationToken);
    }
}
