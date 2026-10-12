using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// Property must have at least a getter or setter.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "PropertyMustHaveGetterOrSetter", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class PropertyMustHaveGetterOrSetterCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PropertyMustHaveGetterOrSetterCode"/> class.
    /// </summary>
    public PropertyMustHaveGetterOrSetterCode()
        : base(21015, "PropertyMustHaveGetterOrSetter",
            ResultSeverities.ByName("Error"),
            "Property must have at least a getter or setter",
            isRetryable: false)
    {
    }
}
