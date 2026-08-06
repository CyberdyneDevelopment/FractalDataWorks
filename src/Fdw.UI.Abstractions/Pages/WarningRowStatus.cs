using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Pages;

/// <summary>Warning status.</summary>
[TypeOption(typeof(RowStatuses), "Warning")]
[ExcludeFromCodeCoverage]
public sealed class WarningRowStatus : RowStatusBase
{
    /// <summary>Initializes a new instance of <see cref="WarningRowStatus"/>.</summary>
    public WarningRowStatus() : base(3, "Warning") { }
}
