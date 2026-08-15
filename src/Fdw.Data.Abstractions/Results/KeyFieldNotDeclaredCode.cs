using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.Abstractions.Results;

/// <summary>
/// A key field names a field the target container does not declare.
/// </summary>
[TypeOption(typeof(ContainerKeyResultCodes), "KeyFieldNotDeclared", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class KeyFieldNotDeclaredCode : ContainerKeyResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KeyFieldNotDeclaredCode"/> class.
    /// </summary>
    public KeyFieldNotDeclaredCode()
        : base(31001, "KeyFieldNotDeclared", ResultSeverities.ByName("Error"),
            "Key field '{FieldName}' is not declared on container '{ContainerName}'",
            isRetryable: false)
    {
    }
}
