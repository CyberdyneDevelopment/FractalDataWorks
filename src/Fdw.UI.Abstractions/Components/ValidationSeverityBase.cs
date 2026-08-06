using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.UI.Abstractions.Components;

/// <summary>Base class for validation message severity.</summary>
// Why: pure TypeOption base — trivial pass-through constructor, no logic to test.
[ExcludeFromCodeCoverage]
public abstract class ValidationSeverityBase : TypeOptionBase<int, ValidationSeverityBase>, IValidationSeverity
{
    /// <summary>Initializes a new instance of <see cref="ValidationSeverityBase"/>.</summary>
    protected ValidationSeverityBase(int id, string name) : base(id, name) { }
}
