using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Pipelines.Clients.Models;

/// <summary>Insert new rows or update existing ones based on key.</summary>
[TypeOption(typeof(WriteModes), "Upsert")]
[ExcludeFromCodeCoverage]
public sealed class UpsertWriteMode : WriteModeBase
{
    /// <summary>Initializes a new instance of <see cref="UpsertWriteMode"/>.</summary>
    public UpsertWriteMode() : base(2, "Upsert") { }
}
