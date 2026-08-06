using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.UI.Abstractions.Pages;

/// <summary>
/// Base class for wizard step status.
/// </summary>
// Why: pure TypeOption base — trivial pass-through constructor, no logic to test.
[ExcludeFromCodeCoverage]
public abstract class WizardStepStatusBase : TypeOptionBase<int, WizardStepStatusBase>, IWizardStepStatus
{
    /// <summary>
    /// Initializes a new instance of <see cref="WizardStepStatusBase"/>.
    /// </summary>
    protected WizardStepStatusBase(int id, string name) : base(id, name) { }
}
