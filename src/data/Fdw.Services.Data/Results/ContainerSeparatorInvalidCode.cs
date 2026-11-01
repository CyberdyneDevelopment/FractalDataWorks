using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>
/// A container states a field separator that is empty or longer than the column holds.
/// </summary>
/// <remarks>
/// Refused at create so the file is never read the wrong way, and the message is written for the caller: it is
/// shown to them (HTTP 400). It holds no backslash, which the message generator does not escape.
/// </remarks>
[TypeOption(typeof(DataServiceResultCodes), "ContainerSeparatorInvalid", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ContainerSeparatorInvalidCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="ContainerSeparatorInvalidCode"/> class.</summary>
    public ContainerSeparatorInvalidCode()
        : base(21021, "ContainerSeparatorInvalid",
            ResultSeverities.ByName("Error"),
            "Container '{container}' states a field separator of {length} characters; a field separator is 1 to 10 characters",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller why the request was refused: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
