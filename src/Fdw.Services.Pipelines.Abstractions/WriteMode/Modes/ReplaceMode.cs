using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Pipelines.Abstractions.WriteMode.Modes;

/// <summary>
/// Replace all existing data with new data.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WriteModes), "Replace")]
public sealed class ReplaceMode : WriteModeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReplaceMode"/> class.
    /// </summary>
    public ReplaceMode() : base(3, "Replace") { }
}
