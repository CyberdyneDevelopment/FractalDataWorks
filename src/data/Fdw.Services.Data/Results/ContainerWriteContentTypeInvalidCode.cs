using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>
/// A container names a content type that is not a media type.
/// </summary>
/// <remarks>
/// Refused at create so the destination is never sent to the wrong place, and the message is written for the caller: it is
/// shown to them (HTTP 400). It holds no backslash, which the message generator does not escape.
/// </remarks>
[TypeOption(typeof(DataServiceResultCodes), "ContainerWriteContentTypeInvalid", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ContainerWriteContentTypeInvalidCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="ContainerWriteContentTypeInvalidCode"/> class.</summary>
    public ContainerWriteContentTypeInvalidCode()
        : base(21025, "ContainerWriteContentTypeInvalid",
            ResultSeverities.ByName("Error"),
            "Container '{container}' names the write content type '{contentType}'; it must be a media type such as application/json",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller why the request was refused: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
