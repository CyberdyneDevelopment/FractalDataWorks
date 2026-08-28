using Fdw.Collections.Attributes;

namespace Fdw.Configuration;

/// <summary>
/// QA / test environment — pre-production validation and testing.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(EnvironmentTypes), "QA")]
public sealed class QaEnvironmentType : EnvironmentTypeBase
{
    /// <summary>
    /// Initializes a new instance of <see cref="QaEnvironmentType"/>.
    /// </summary>
    public QaEnvironmentType() : base(3, "QA")
    {
    }
}
