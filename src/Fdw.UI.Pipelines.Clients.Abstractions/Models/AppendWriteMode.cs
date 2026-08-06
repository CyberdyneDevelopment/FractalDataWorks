using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Pipelines.Clients.Models;

/// <summary>Append data to existing data.</summary>
[TypeOption(typeof(WriteModes), "Append")]
[ExcludeFromCodeCoverage]
public sealed class AppendWriteMode : WriteModeBase
{
    /// <summary>Initializes a new instance of <see cref="AppendWriteMode"/>.</summary>
    public AppendWriteMode() : base(4, "Append") { }
}
