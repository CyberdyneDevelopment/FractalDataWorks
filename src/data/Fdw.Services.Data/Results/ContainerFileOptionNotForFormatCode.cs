using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>
/// A container states a file option (HasHeader, Separator, SheetName) that its format does not take.
/// </summary>
/// <remarks>
/// Refused at create so the file is never read the wrong way, and the message is written for the caller: it is
/// shown to them (HTTP 400). It holds no backslash, which the message generator does not escape.
/// </remarks>
[TypeOption(typeof(DataServiceResultCodes), "ContainerFileOptionNotForFormat", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ContainerFileOptionNotForFormatCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="ContainerFileOptionNotForFormatCode"/> class.</summary>
    public ContainerFileOptionNotForFormatCode()
        : base(21019, "ContainerFileOptionNotForFormat",
            ResultSeverities.ByName("Error"),
            "Container '{container}' states {option}, which only a {formats} container takes",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller why the request was refused: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
