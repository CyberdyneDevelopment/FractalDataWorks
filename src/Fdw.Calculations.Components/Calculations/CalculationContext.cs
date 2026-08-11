#pragma warning disable CS1591
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Web.Calculations.Clients.Models;
using Fdw.UI.Providers;

namespace Fdw.Calculations.Components.Calculations;

public sealed class CalculationContext : ProviderContextBase
{
    // ── State ──────────────────────────────────────────────────────────────────

    public IReadOnlyList<CalculationSummaryPayload> Calculations { get; init; } = [];
    public string SearchString { get; init; } = string.Empty;

    // ── Derived ────────────────────────────────────────────────────────────────

    public IReadOnlyList<CalculationSummaryPayload> FilteredCalculations { get; init; } = [];

    // ── Callbacks ──────────────────────────────────────────────────────────────

    public Func<Task> OnLoadCalculations { get; init; } = () => Task.CompletedTask;
    public Func<string, Task> OnSearchChanged { get; init; } = _ => Task.CompletedTask;
    public Func<Guid, Task<CalculationDetailPayload?>> OnGetCalculationDetails { get; init; } = _ => Task.FromResult<CalculationDetailPayload?>(null);
    public Func<CreateCalculationDefinitionRequest, Task<CalculationDetailPayload?>> OnCreateCalculation { get; init; } = _ => Task.FromResult<CalculationDetailPayload?>(null);
    public Func<Guid, UpdateCalculationDefinitionRequest, Task<CalculationDetailPayload?>> OnUpdateCalculation { get; init; } = (_, _) => Task.FromResult<CalculationDetailPayload?>(null);
    public Func<Guid, Task<bool>> OnDeleteCalculation { get; init; } = _ => Task.FromResult(false);
    public Func<string, string, Task<PreviewFormulaResponse?>> OnValidateFormula { get; init; } = (_, _) => Task.FromResult<PreviewFormulaResponse?>(null);
    public Func<string, Task<IReadOnlyList<DataSetFieldPayload>?>> OnGetDataSetFields { get; init; } = _ => Task.FromResult<IReadOnlyList<DataSetFieldPayload>?>(null);
    public Func<Task<CalculationTypesResponse?>> OnGetCalculationTypes { get; init; } = () => Task.FromResult<CalculationTypesResponse?>(null);
    public Func<PreviewCalculationRequest, Task<PreviewCalculationResponse?>> OnPreviewCalculation { get; init; } = _ => Task.FromResult<PreviewCalculationResponse?>(null);
    public Func<ExecuteCalculationRequest, Task<ExecuteCalculationResponse?>> OnExecuteCalculation { get; init; } = _ => Task.FromResult<ExecuteCalculationResponse?>(null);
}
