using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Pages;

/// <summary>Informational activity.</summary>
[TypeOption(typeof(ActivitySeverities), "Info")]
[ExcludeFromCodeCoverage]
public sealed class InfoActivitySeverity : ActivitySeverityBase
{
    /// <summary>Initializes a new instance of <see cref="InfoActivitySeverity"/>.</summary>
    public InfoActivitySeverity() : base(1, "Info") { }
}
