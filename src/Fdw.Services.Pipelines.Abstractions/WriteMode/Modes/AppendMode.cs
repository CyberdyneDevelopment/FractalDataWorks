using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Pipelines.Abstractions.WriteMode.Modes;

/// <summary>
/// Append data to existing data.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WriteModes), "Append")]
public sealed class AppendMode : WriteModeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AppendMode"/> class.
    /// </summary>
    public AppendMode() : base(4, "Append") { }
}
