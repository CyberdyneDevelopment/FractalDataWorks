using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Tags.Results;

/// <summary>No tag named 'name' is visible to the caller (FDW-3669).</summary>
[TypeOption(typeof(TagsResultCodes), "TagNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class TagNotFoundCode : TagsResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="TagNotFoundCode"/> class.</summary>
    public TagNotFoundCode()
        : base(31301, "TagNotFound",
            ResultSeverities.ByName("Error"),
            "No tag named '{name}' is visible to the caller",
            isRetryable: false)
    {
    }

    /// <inheritdoc />
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
