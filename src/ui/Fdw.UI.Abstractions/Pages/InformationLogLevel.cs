using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Pages;

/// <summary>Informational logging.</summary>
[TypeOption(typeof(LogLevels), "Information")]
[ExcludeFromCodeCoverage]
public sealed class InformationLogLevel : LogLevelBase
{
    /// <summary>Initializes a new instance of <see cref="InformationLogLevel"/>.</summary>
    public InformationLogLevel() : base(2, "Information") { }
}
