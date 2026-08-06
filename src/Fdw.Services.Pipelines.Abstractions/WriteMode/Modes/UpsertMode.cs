using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Pipelines.Abstractions.WriteMode.Modes;

/// <summary>
/// Insert new rows or update existing ones based on key.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WriteModes), "Upsert")]
public sealed class UpsertMode : WriteModeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpsertMode"/> class.
    /// </summary>
    public UpsertMode() : base(2, "Upsert") { }
}
