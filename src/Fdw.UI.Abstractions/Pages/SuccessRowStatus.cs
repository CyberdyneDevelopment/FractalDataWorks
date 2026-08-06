using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Pages;

/// <summary>Success/Active status.</summary>
[TypeOption(typeof(RowStatuses), "Success")]
[ExcludeFromCodeCoverage]
public sealed class SuccessRowStatus : RowStatusBase
{
    /// <summary>Initializes a new instance of <see cref="SuccessRowStatus"/>.</summary>
    public SuccessRowStatus() : base(2, "Success") { }
}
