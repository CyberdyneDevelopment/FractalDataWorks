using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;

namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>
/// A configured language model: a reply to a request, or the reply as a stream of events. The contract names no provider; a provider is
/// an implementation of the language model domain.
/// </summary>
public interface ILanguageModel : IServiceOption
{
    /// <summary>Gets the id the provider knows the model by. It is set on the model's configuration row, never in code.</summary>
    string ModelId { get; }

    /// <summary>Gets the names of the <see cref="ModelCapabilities"/> options the model carries, from its configuration row.</summary>
    IReadOnlyCollection<string> Capabilities { get; }

    /// <summary>Asks the model for a whole reply.</summary>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    /// <returns>The reply, or a failure with a coded message. A request the model cannot carry is refused here, before any call.</returns>
    Task<IGenericResult<LanguageModelReply>> Reply(LanguageModelRequest request, CancellationToken cancellationToken = default);

    /// <summary>Asks the model for its reply as a stream of events.</summary>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    /// <returns>The events. A failure, including the refusal of a request the model cannot carry, is a stream of one failed event.</returns>
    IAsyncEnumerable<LanguageModelStreamEvent> Stream(LanguageModelRequest request, CancellationToken cancellationToken = default);
}
