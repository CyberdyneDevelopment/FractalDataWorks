using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Pages;

/// <summary>Disabled/Inactive status.</summary>
[TypeOption(typeof(RowStatuses), "Disabled")]
[ExcludeFromCodeCoverage]
public sealed class DisabledRowStatus : RowStatusBase
{
    /// <summary>Initializes a new instance of <see cref="DisabledRowStatus"/>.</summary>
    public DisabledRowStatus() : base(5, "Disabled") { }
}
