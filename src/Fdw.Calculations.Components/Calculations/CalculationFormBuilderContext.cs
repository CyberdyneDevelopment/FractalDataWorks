#pragma warning disable CS1591
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Web.Calculations.Clients.Models;
using Fdw.UI.Providers;

namespace Fdw.Calculations.Components.Calculations;

/// <summary>
/// State and callbacks for the <see cref="CalculationFormBuilder"/> component.
/// All properties are read-only init — the parent provider rebuilds and re-renders on each change.
/// </summary>
public sealed class CalculationFormBuilderContext : ProviderContextBase
{
    // ── State ──────────────────────────────────────────────────────────────────

    /// <summary>Gets the calculation being edited, or null when creating a new calculation.</summary>
    public CalculationDetailPayload? FormState { get; init; }



    /// <summary>Gets the currently selected formula language (e.g. "CSharp" or "Sql").</summary>
    public string SelectedLanguage { get; init; } = "CSharp";

    /// <summary>Gets the result of the most recent formula preview/validation, or null if none yet.</summary>
    public PreviewFormulaResponse? PreviewResult { get; init; }

    /// <summary>Gets the available DataSet fields for the current TargetDataSet, or null if not loaded.</summary>
    public IReadOnlyList<DataSetFieldPayload>? DataSetFields { get; init; }

    // ── Callbacks ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Callback: validate the given formula against the named DataSet.
    /// Parameters: formula (string), dataSetName (string).
    /// Returns the validation response, or null on failure.
    /// </summary>
    public Func<string, string, Task<PreviewFormulaResponse?>> OnValidateFormula { get; init; } =
        (_, _) => Task.FromResult<PreviewFormulaResponse?>(null);

    /// <summary>
    /// Callback: load the field list for the given DataSet name.
    /// Returns the fields, or null on failure.
    /// </summary>
    public Func<string, Task<IReadOnlyList<DataSetFieldPayload>?>> OnGetDataSetFields { get; init; } =
        _ => Task.FromResult<IReadOnlyList<DataSetFieldPayload>?>(null);

    /// <summary>
    /// Callback: notify the parent that the language selection changed.
    /// The parent rebuilds state so <see cref="SelectedLanguage"/> stays consistent.
    /// </summary>
    public Func<string, Task> OnLanguageChanged { get; init; } = _ => Task.CompletedTask;
}
