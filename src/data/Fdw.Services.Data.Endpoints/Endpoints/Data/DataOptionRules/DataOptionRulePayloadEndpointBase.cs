using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Data.Abstractions.DataConfigurationRules;
using Fdw.Services.Data.Clients.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// A data option rule route that answers one rule (FDW-6489): the service's rule becomes the payload with the names of its owner, custodian and changer, or the failure of the
/// service, or of the names directory when it cannot name one.
/// </summary>
/// <typeparam name="TRequest">The request.</typeparam>
public abstract class DataOptionRulePayloadEndpointBase<TRequest> : DataOptionRuleServiceEndpointBase<TRequest, DataOptionRulePayload>
    where TRequest : notnull
{
    /// <summary>Initializes a new instance of the <see cref="DataOptionRulePayloadEndpointBase{TRequest}"/> class.</summary>
    /// <param name="logger">The logger of the concrete endpoint.</param>
    /// <param name="rules">The rule service.</param>
    /// <param name="names">The names directory.</param>
    protected DataOptionRulePayloadEndpointBase(ILogger? logger, IDataConfigurationRules rules, IDataConfigurationRuleNames names)
        : base(logger)
    {
        Rules = rules;
        Names = names;
    }

    /// <summary>Gets the rule service.</summary>
    protected IDataConfigurationRules Rules { get; }

    /// <summary>Gets the names directory.</summary>
    protected IDataConfigurationRuleNames Names { get; }

    /// <summary>Maps the service's answer to the payload.</summary>
    /// <param name="answer">The service's answer.</param>
    /// <param name="cancellationToken">Cancels the read of the names.</param>
    /// <returns>The payload; or why there is none.</returns>
    protected async Task<IGenericResult<DataOptionRulePayload>> Payload(IGenericResult<DataConfigurationRule> answer, CancellationToken cancellationToken)
        => answer.IsSuccess && answer.Value is { } rule
            ? await DataOptionRuleMapping.Payload(rule, Names, cancellationToken).ConfigureAwait(false)
            : answer.ToNewResult<DataOptionRulePayload>();

    /// <summary>A failure of this route with a coded message of its own.</summary>
    /// <param name="message">The message.</param>
    /// <returns>The failure.</returns>
    protected static IGenericResult<DataOptionRulePayload> Refused(IGenericMessage message) => GenericResult<DataOptionRulePayload>.Failure(message);
}
