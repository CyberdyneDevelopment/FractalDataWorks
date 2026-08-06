using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// No fields found to generate constructor parameters.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "NoFieldsFoundToGenerateConstructorParameters", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class NoFieldsFoundToGenerateConstructorParametersCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NoFieldsFoundToGenerateConstructorParametersCode"/> class.
    /// </summary>
    public NoFieldsFoundToGenerateConstructorParametersCode()
        : base(31004, "NoFieldsFoundToGenerateConstructorParameters",
            ResultSeverities.ByName("Error"),
            "No fields found to generate constructor parameters",
            isRetryable: false)
    {
    }
}
