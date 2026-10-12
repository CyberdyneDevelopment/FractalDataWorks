using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Pages;

/// <summary>Success/completion activity.</summary>
[TypeOption(typeof(ActivitySeverities), "Success")]
[ExcludeFromCodeCoverage]
public sealed class SuccessActivitySeverity : ActivitySeverityBase
{
    /// <summary>Initializes a new instance of <see cref="SuccessActivitySeverity"/>.</summary>
    public SuccessActivitySeverity() : base(2, "Success") { }
}
