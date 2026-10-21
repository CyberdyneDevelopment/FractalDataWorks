using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>
/// A container names an HTTP method that a destination cannot send its rows with.
/// </summary>
/// <remarks>
/// Refused at create so the destination is never sent to the wrong place, and the message is written for the caller: it is
/// shown to them (HTTP 400). It holds no backslash, which the message generator does not escape.
/// </remarks>
[TypeOption(typeof(DataServiceResultCodes), "ContainerWriteMethodInvalid", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ContainerWriteMethodInvalidCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="ContainerWriteMethodInvalidCode"/> class.</summary>
    public ContainerWriteMethodInvalidCode()
        : base(21023, "ContainerWriteMethodInvalid",
            ResultSeverities.ByName("Error"),
            "Container '{container}' names the write method '{method}'; a container sends its rows with POST or PUT",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller why the request was refused: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
