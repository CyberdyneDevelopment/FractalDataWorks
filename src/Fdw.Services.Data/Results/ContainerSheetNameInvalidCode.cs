using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>
/// A container names a worksheet that Excel cannot hold.
/// </summary>
/// <remarks>
/// Refused at create so the file is never read the wrong way, and the message is written for the caller: it is
/// shown to them (HTTP 400). It holds no backslash, which the message generator does not escape.
/// </remarks>
[TypeOption(typeof(DataServiceResultCodes), "ContainerSheetNameInvalid", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ContainerSheetNameInvalidCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="ContainerSheetNameInvalidCode"/> class.</summary>
    public ContainerSheetNameInvalidCode()
        : base(21020, "ContainerSheetNameInvalid",
            ResultSeverities.ByName("Error"),
            "Container '{container}' names the worksheet '{sheetName}', which Excel cannot hold: a worksheet name is 1 to 31 characters and has none of the characters [ ] : * ? / or a backslash",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller why the request was refused: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
