using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Schema.Ddl.Results;

/// <summary>
/// A name that is to be written into DDL cannot be used by the target dialect: it is empty or blank, or it holds a NUL character (FDW-3617).
/// </summary>
/// <remarks>
/// The generator refuses before it writes any text, naming the name and why, rather than quoting it into something the engine would read
/// differently from what was asked. Every other character, a closing bracket and a quote included, is escaped and is that literal name.
/// </remarks>
[TypeOption(typeof(DdlResultCodes), "IdentifierInvalid", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class IdentifierInvalidCode : DdlResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IdentifierInvalidCode"/> class.
    /// </summary>
    public IdentifierInvalidCode()
        : base(20002, "IdentifierInvalid",
            ResultSeverities.ByName("Error"),
            "The name '{identifier}' cannot be written into {dialect} DDL: {reason}",
            isRetryable: false)
    {
    }
}
