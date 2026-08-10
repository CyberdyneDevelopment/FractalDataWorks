using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Pipelines.Clients.Models;

/// <summary>Insert new rows only.</summary>
[TypeOption(typeof(WriteModes), "Insert")]
[ExcludeFromCodeCoverage]
public sealed class InsertWriteMode : WriteModeBase
{
    /// <summary>Initializes a new instance of <see cref="InsertWriteMode"/>.</summary>
    public InsertWriteMode() : base(1, "Insert") { }
}
