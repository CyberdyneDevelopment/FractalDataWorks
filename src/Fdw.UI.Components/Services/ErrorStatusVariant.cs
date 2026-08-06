using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Services;

/// <summary>Error variant.</summary>
[TypeOption(typeof(StatusVariants), "Error")]
[ExcludeFromCodeCoverage]
public sealed class ErrorStatusVariant : StatusVariantBase
{
    /// <summary>Initializes a new instance of <see cref="ErrorStatusVariant"/>.</summary>
    public ErrorStatusVariant() : base(2, "Error") { }
}
