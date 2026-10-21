using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>
/// A container names a write endpoint that is not a path after the connection's address.
/// </summary>
/// <remarks>
/// Refused at create so the destination is never sent to the wrong place, and the message is written for the caller: it is
/// shown to them (HTTP 400). It holds no backslash, which the message generator does not escape.
/// </remarks>
[TypeOption(typeof(DataServiceResultCodes), "ContainerWriteEndpointInvalid", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ContainerWriteEndpointInvalidCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="ContainerWriteEndpointInvalidCode"/> class.</summary>
    public ContainerWriteEndpointInvalidCode()
        : base(21024, "ContainerWriteEndpointInvalid",
            ResultSeverities.ByName("Error"),
            "Container '{container}' names the write endpoint '{endpoint}'; it must be a path after the connection's address, such as /api/v1/ingest, not a full web address",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller why the request was refused: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
