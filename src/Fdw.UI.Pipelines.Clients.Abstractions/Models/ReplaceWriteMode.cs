using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Pipelines.Clients.Models;

/// <summary>Replace all existing data with new data.</summary>
[TypeOption(typeof(WriteModes), "Replace")]
[ExcludeFromCodeCoverage]
public sealed class ReplaceWriteMode : WriteModeBase
{
    /// <summary>Initializes a new instance of <see cref="ReplaceWriteMode"/>.</summary>
    public ReplaceWriteMode() : base(3, "Replace") { }
}
