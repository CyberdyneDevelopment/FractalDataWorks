using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Fdw.Analyzers.Tests;

/// <summary>
/// FDW057 (FDW-3580): <c>result.FailureReason()</c> is read only where the result is known to have failed, the mirror
/// of FDW016's guard for Value.
/// </summary>
public class UncheckedFailureReasonAnalyzerTests
{
    private const string Stubs = @"
using System;
using Fdw.Results;

namespace Fdw.Messages
{
    public interface IGenericMessage { string Message { get; } }
}

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

    public static class FailureMessageExtensions
    {
        public static Fdw.Messages.IGenericMessage FailureReason(this IGenericResult result) => throw new InvalidOperationException();
    }
}
";

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Analyzer")]
    public async Task AReadInsideAFailureCheckIsSilent()
    {
        var source = Stubs + @"
class Test
{
    string InNegatedSuccess(IGenericResult r)
    {
        if (!r.IsSuccess)
            return r.FailureReason().Message;
        return ""ok"";
    }

    string InFailure(IGenericResult r)
    {
        if (r.IsFailure)
        {
            return r.FailureReason().Message;
        }

        return ""ok"";
    }

    string InElseOfSuccess(IGenericResult r)
    {
        if (r.IsSuccess)
            return ""ok"";
        else
            return r.FailureReason().Message;
    }

    string AfterSuccessExit(IGenericResult r)
    {
        if (r.IsSuccess)
            return ""ok"";

        return r.FailureReason().Message;
    }

    string InPattern(IGenericResult r) => r is { IsSuccess: false } ? r.FailureReason().Message : ""ok"";

    string InTernaryElse(IGenericResult r) => r.IsSuccess ? ""ok"" : r.FailureReason().Message;

    bool InShortCircuitAnd(IGenericResult r) => r.IsFailure && r.FailureReason().Message.Length > 0;

    bool InShortCircuitOr(IGenericResult r) => r.IsSuccess || r.FailureReason().Message.Length > 0;

    string StaticForm(IGenericResult r) => r.IsFailure ? FailureMessageExtensions.FailureReason(r).Message : ""ok"";
}";

        var test = new CSharpAnalyzerTest<UncheckedFailureReasonAnalyzer, DefaultVerifier>
        {
            TestCode = source,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Analyzer")]
    public async Task AReadThatMayBeOnASuccessReports()
    {
        var source = Stubs + @"
class Test
{
    string Unchecked(IGenericResult r) => {|#0:r.FailureReason()|}.Message;

    string NullValueBranch(IGenericResult<string> r)
    {
        if (!r.IsSuccess || r.Value is null)
            return {|#1:r.FailureReason()|}.Message;
        return r.Value;
    }

    string InSuccess(IGenericResult r)
    {
        if (r.IsSuccess)
            return {|#2:r.FailureReason()|}.Message;
        return ""failed"";
    }

    string AfterFailureExit(IGenericResult r)
    {
        if (!r.IsSuccess)
            return ""failed"";

        return {|#3:r.FailureReason()|}.Message;
    }
}";

        var test = new CSharpAnalyzerTest<UncheckedFailureReasonAnalyzer, DefaultVerifier>
        {
            TestCode = source,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };
        for (var location = 0; location < 4; location++)
        {
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(UncheckedFailureReasonAnalyzer.DiagnosticId, DiagnosticSeverity.Error)
                    .WithLocation(location)
                    .WithArguments("r"));
        }

        await test.RunAsync(TestContext.Current.CancellationToken);
    }
}
