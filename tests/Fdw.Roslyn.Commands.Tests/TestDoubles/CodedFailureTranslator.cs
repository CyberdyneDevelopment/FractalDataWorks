using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Roslyn.Commands.Abstractions;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Microsoft.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Tests.TestDoubles;

/// <summary>
/// A translator whose <see cref="Translate"/> fails with a specific result code.
/// </summary>
/// <remarks>
/// Exists to pin the boundary between <c>Translate</c> and <c>Execute</c>. Tests that call
/// <c>Translate</c> directly never cross it, so a base class that discarded the code on the way out
/// stayed invisible to the whole suite while every real caller — which goes through <c>Execute</c> —
/// received Code=null.
/// </remarks>
public sealed class CodedFailureTranslator : RoslynCommandTranslatorBase<FakeRoslynCommand, FakeCommandResult>
{
    /// <summary>The code this translator fails with.</summary>
    public const string FailureCode = "NoTypesMatchedSelector";

    /// <summary>Initializes a new instance of the <see cref="CodedFailureTranslator"/> class.</summary>
    public CodedFailureTranslator()
        : base("CodedFailure", "Always fails, with a code.")
    {
    }

    /// <inheritdoc/>
    public override Task<IGenericResult<FakeCommandResult>> Translate(
        FakeRoslynCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(GenericResult<FakeCommandResult>.Failure(
            RoslynResultCodes.ByName(FailureCode),
            ResultDetails.Create().With("Message", "nothing matched")));
    }
}
