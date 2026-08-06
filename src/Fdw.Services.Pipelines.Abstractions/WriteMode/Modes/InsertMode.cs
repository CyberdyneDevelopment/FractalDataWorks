using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Pipelines.Abstractions.WriteMode.Modes;

/// <summary>
/// Insert new rows only.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WriteModes), "Insert")]
public sealed class InsertMode : WriteModeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InsertMode"/> class.
    /// </summary>
    public InsertMode() : base(1, "Insert") { }
}
