using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Tags.Results;

/// <summary>The thing a tag was to be put on was retired or deleted, so it cannot be tagged (FDW-6221).</summary>
/// <remarks>
/// A thing that is gone is a different answer from one that was never there (<c>TagThingNotFound</c>): the caller brings the first back and corrects the id of
/// the second. Tags a thing already carries when it is retired stay readable and can be taken off. HTTP 409.
/// </remarks>
[TypeOption(typeof(TagsResultCodes), "TagThingRetired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class TagThingRetiredCode : TagsResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="TagThingRetiredCode"/> class.</summary>
    public TagThingRetiredCode()
        : base(41310, "TagThingRetired",
            ResultSeverities.ByName("Error"),
            "The {kind} '{thingId}' was retired, so it cannot be tagged",
            isRetryable: false)
    {
    }

    /// <inheritdoc />
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
