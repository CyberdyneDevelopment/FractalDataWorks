using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Pages;

/// <summary>
/// TypeCollection for wizard step status values.
/// </summary>
[TypeCollection(typeof(WizardStepStatusBase), typeof(IWizardStepStatus), typeof(WizardStepStatuses))]
[ExcludeFromCodeCoverage]
public abstract partial class WizardStepStatuses : TypeCollectionBase<WizardStepStatusBase, IWizardStepStatus> { }
