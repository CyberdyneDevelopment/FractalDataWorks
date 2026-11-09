using System;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Abstractions;

/// <summary>
/// One service a service type or a collection needs registered, and the message that says so when it is not (FDW-4588).
/// </summary>
/// <remarks>
/// Why the message is a method and not text: each missing service is its own failure with its own code and client text, so the declaration names the
/// <c>[MessageLogging]</c> method (<c>Requires&lt;IThresholdEvaluator&gt;(RunThresholdLog.EvaluatorNotRegistered)</c>) and the check calls it with the owner's logger.
/// </remarks>
public sealed class ServiceRequirement
{
    /// <summary>Initializes a new instance of the <see cref="ServiceRequirement"/> class.</summary>
    /// <param name="service">The service that must be registered.</param>
    /// <param name="message">The coded message logged and returned when it is not.</param>
    public ServiceRequirement(Type service, Func<ILogger, IGenericMessage> message)
    {
        Service = service ?? throw new ArgumentNullException(nameof(service));
        Message = message ?? throw new ArgumentNullException(nameof(message));
    }

    /// <summary>Gets the service that must be registered.</summary>
    public Type Service { get; }

    /// <summary>Gets the coded message for the service being absent.</summary>
    public Func<ILogger, IGenericMessage> Message { get; }
}
