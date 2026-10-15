using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>
/// A container of a store that addresses its containers as files is named with its format's own extension.
/// </summary>
/// <remarks>
/// Refused at create so the file is never read the wrong way, and the message is written for the caller: it is
/// shown to them (HTTP 400). It holds no backslash, which the message generator does not escape.
/// </remarks>
[TypeOption(typeof(DataServiceResultCodes), "ContainerNameCarriesFormatExtension", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ContainerNameCarriesFormatExtensionCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="ContainerNameCarriesFormatExtensionCode"/> class.</summary>
    public ContainerNameCarriesFormatExtensionCode()
        : base(21022, "ContainerNameCarriesFormatExtension",
            ResultSeverities.ByName("Error"),
            "Container '{container}' is named with its format's own extension '{extension}'; the store adds the extension itself, so name the container '{expected}'",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller why the request was refused: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
