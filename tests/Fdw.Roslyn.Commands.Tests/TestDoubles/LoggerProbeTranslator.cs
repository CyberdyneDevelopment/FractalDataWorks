using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Roslyn.Commands.Abstractions;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Tests.TestDoubles;

/// <summary>
/// A base-derived translator that exposes the logger it was given.
/// </summary>
/// <remarks>
/// <c>Logger</c> is protected internal, so only a subclass can see it — which is the point. The
/// question this double exists to answer is whether registration actually reaches the instance the
/// registry hands out, and there is no way to observe that from outside the inheritance chain.
/// </remarks>
public sealed class LoggerProbeTranslator : RoslynCommandTranslatorBase<FakeRoslynCommand, FakeCommandResult>
{
    /// <summary>Initializes a new instance of the <see cref="LoggerProbeTranslator"/> class.</summary>
    public LoggerProbeTranslator()
        : base("LoggerProbe", "Reports the logger it was handed.")
    {
    }

    /// <summary>The line this translator emits when it runs.</summary>
    public const string Marker = "LoggerProbeTranslator ran";

    /// <summary>Gets the logger currently attached to this translator.</summary>
    public ILogger AttachedLogger => Logger;

    /// <inheritdoc/>
    public override Task<IGenericResult<FakeCommandResult>> Translate(
        FakeRoslynCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        // Emitted through the base's Logger, exactly as a real translator does, so the test observes the
        // same path production uses rather than a property read.
#pragma warning disable CA1848 // Use the LoggerMessage delegates — this double exists to exercise the plain path.
        Logger.LogInformation(Marker);
#pragma warning restore CA1848

        return Task.FromResult(GenericResult<FakeCommandResult>.Success(new FakeCommandResult()));
    }
}
