using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Tags.Results;

/// <summary>'name' is not a valid tag name (FDW-3669).</summary>
[TypeOption(typeof(TagsResultCodes), "TagNameInvalid", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class TagNameInvalidCode : TagsResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="TagNameInvalidCode"/> class.</summary>
    public TagNameInvalidCode()
        : base(21302, "TagNameInvalid",
            ResultSeverities.ByName("Error"),
            "'{name}' is not a valid tag name: {reason}",
            isRetryable: false)
    {
    }

    /// <inheritdoc />
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
