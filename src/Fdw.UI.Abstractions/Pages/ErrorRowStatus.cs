using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Pages;

/// <summary>Error/Failed status.</summary>
[TypeOption(typeof(RowStatuses), "Error")]
[ExcludeFromCodeCoverage]
public sealed class ErrorRowStatus : RowStatusBase
{
    /// <summary>Initializes a new instance of <see cref="ErrorRowStatus"/>.</summary>
    public ErrorRowStatus() : base(4, "Error") { }
}
