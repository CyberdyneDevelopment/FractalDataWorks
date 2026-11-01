using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.Abstractions.Results;

/// <summary>
/// A Foreign key was declared without naming the container it references.
/// </summary>
[TypeOption(typeof(ContainerKeyResultCodes), "ForeignKeyMissingReference", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ForeignKeyMissingReferenceCode : ContainerKeyResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ForeignKeyMissingReferenceCode"/> class.
    /// </summary>
    public ForeignKeyMissingReferenceCode()
        : base(21001, "ForeignKeyMissingReference", ResultSeverities.ByName("Error"),
            "Foreign key '{KeyName}' on container '{ContainerName}' does not name a referenced container",
            isRetryable: false)
    {
    }
}
