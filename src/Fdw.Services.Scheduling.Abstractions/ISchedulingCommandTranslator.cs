using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Results;

namespace Fdw.Services.Scheduling.Abstractions;

/// <summary>
/// Interface for scheduling command translators. Translators convert a universal
/// <see cref="ISchedulingCommand"/> into an implementation's native call — mirrors
/// <c>IDataCommandTranslator&lt;TCommand&gt;</c> for connections.
/// </summary>
/// <typeparam name="TNative">The native type this translator produces (e.g. Quartz's <c>ITrigger</c>,
/// Hangfire's recurring-job call shape).</typeparam>
/// <remarks>
/// One command per operation, shared by every implementation. Only the translator varies per
/// implementation — each implementation collects its own family of translators in its own
/// implementation-scoped TypeCollection (one <c>[TypeOption]</c> per command kind), the way
/// <c>MsSqlDataCommandTranslators</c> does for SQL commands.
/// </remarks>
public interface ISchedulingCommandTranslator<TNative> : ITypeOption<int>
{
    /// <summary>
    /// Gets the implementation name this translator targets (e.g. "Quartz", "Hangfire").
    /// </summary>
    string DomainName { get; }

    /// <summary>
    /// Translates a scheduling command to that implementation's native call.
    /// </summary>
    /// <param name="command">The scheduling command to translate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the translated native value.</returns>
    Task<IGenericResult<TNative>> Translate(
        ISchedulingCommand command,
        CancellationToken cancellationToken = default);
}
