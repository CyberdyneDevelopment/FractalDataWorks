using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Abstractions.Audit;

/// <summary>
/// Platform: the whole system, run by the operator.
/// </summary>
[TypeOption(typeof(AdministrativeLevels), "Platform", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class PlatformAdministrativeLevel : AdministrativeLevelBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlatformAdministrativeLevel"/> class.
    /// </summary>
    public PlatformAdministrativeLevel()
        : base("Platform", 0)
    {
    }
}
