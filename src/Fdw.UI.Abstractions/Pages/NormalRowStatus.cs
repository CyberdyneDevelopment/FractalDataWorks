using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Pages;

/// <summary>Normal row.</summary>
[TypeOption(typeof(RowStatuses), "Normal")]
[ExcludeFromCodeCoverage]
public sealed class NormalRowStatus : RowStatusBase
{
    /// <summary>Initializes a new instance of <see cref="NormalRowStatus"/>.</summary>
    public NormalRowStatus() : base(1, "Normal") { }
}
