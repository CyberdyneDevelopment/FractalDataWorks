using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Descriptions.Abstractions;

/// <summary>A theme's description: what the theme is for, in the words of whoever configured it (FDW-6208). Read by whoever holds <c>themes:read</c>. The theme is the thing described, so the type has no owner: its descriptions are removed with it. The provider never fills the text on load: the endpoint that shows it reads it once for its answer.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DescriptionTypes), OptionName)]
public sealed class ThemeDescriptionType : DescriptionTypeBase
{
    /// <summary>The type's name, which the provider writes and the endpoint reads its descriptions under.</summary>
    public const string OptionName = "Theme";

    /// <summary>Initializes a new instance of the <see cref="ThemeDescriptionType"/> class.</summary>
    public ThemeDescriptionType()
        : base(OptionName, "configurations:read", hasOwner: false)
    {
    }
}
