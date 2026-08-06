using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Rendering.Spectre.PageRenderers;

/// <summary>
/// TypeCollection for wizard navigation actions.
/// </summary>
[TypeCollection(typeof(WizardActionBase), typeof(IWizardAction), typeof(WizardActions))]
[ExcludeFromCodeCoverage]
public abstract partial class WizardActions : TypeCollectionBase<WizardActionBase, IWizardAction> { }
