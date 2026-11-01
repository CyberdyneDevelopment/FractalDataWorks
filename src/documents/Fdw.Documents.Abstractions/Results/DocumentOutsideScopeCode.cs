using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Documents.Abstractions.Results;

/// <summary>The path 'Path' is outside the scope 'Scope' of the connection. (Validation, 20450.)</summary>
[TypeOption(typeof(DocumentResultCodes), "DocumentOutsideScope", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DocumentOutsideScopeCode : DocumentResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="DocumentOutsideScopeCode"/> class.</summary>
    public DocumentOutsideScopeCode()
        : base(20450, "DocumentOutsideScope",
            ResultSeverities.ByName("Error"),
            "The path '{Path}' is outside the scope '{Scope}' of the connection.",
            isRetryable: false)
    {
    }
}
