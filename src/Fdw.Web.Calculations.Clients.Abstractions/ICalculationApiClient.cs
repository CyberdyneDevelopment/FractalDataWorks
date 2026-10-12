using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Web.Calculations.Clients.CalculationEntities;
using Fdw.Web.Calculations.Clients.Formula;
using Fdw.Web.Calculations.Clients.Models;

namespace Fdw.Web.Calculations.Clients;

/// <summary>
/// Contract for the calculation API client covering CRUD, execution, formula tooling,
/// and bulk DataSet field enumeration.
/// </summary>
public interface ICalculationApiClient
{
    /// <summary>
    /// Gets all available calculation types.
    /// </summary>
    Task<IGenericResult<CalculationTypesResponse>> GetCalculationTypes(CancellationToken ct = default);

    /// <summary>Gets the period comparison types a calculation can be evaluated across.</summary>
    /// <param name="ct">A token to cancel the request.</param>
    /// <returns>A result containing the available period comparison types.</returns>
    Task<IGenericResult<PeriodComparisonTypesResponse>> GetPeriodComparisonTypes(CancellationToken ct = default);

    /// <summary>
    /// Executes a calculation with the specified input values.
    /// </summary>
    Task<IGenericResult<ExecuteCalculationResponse>> ExecuteCalculation(ExecuteCalculationRequest request, CancellationToken ct = default);

    /// <summary>
    /// Gets all defined calculations.
    /// </summary>
    Task<IGenericResult<IReadOnlyList<CalculationSummaryPayload>>> GetCalculations(CancellationToken ct = default);

    /// <summary>
    /// Gets a specific calculation definition by identifier.
    /// </summary>
    Task<IGenericResult<CalculationDetailPayload>> GetCalculation(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Creates a new calculation definition.
    /// </summary>
    Task<IGenericResult<CalculationDetailPayload>> CreateCalculation(CreateCalculationDefinitionRequest request, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing calculation definition.
    /// </summary>
    Task<IGenericResult<CalculationDetailPayload>> UpdateCalculation(Guid id, UpdateCalculationDefinitionRequest request, CancellationToken ct = default);

    /// <summary>
    /// Deletes a calculation definition.
    /// </summary>
    Task<IGenericResult> DeleteCalculation(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Validates a calculation formula.
    /// </summary>
    Task<IGenericResult<PreviewFormulaResponse>> ValidateFormula(ValidateFormulaPayload request, CancellationToken ct = default);

    /// <summary>
    /// Previews a calculation with sample data.
    /// </summary>
    Task<IGenericResult<PreviewCalculationResponse>> PreviewCalculation(PreviewCalculationRequest request, CancellationToken ct = default);

    /// <summary>
    /// Gets the fields for a specific DataSet.
    /// </summary>
    Task<IGenericResult<IReadOnlyList<DataSetFieldPayload>>> GetDataSetFields(string dataSetName, CancellationToken ct = default);

    /// <summary>
    /// Gets fields for all DataSets in a single call.
    /// Avoids N+1 requests when the formula editor needs field lists from every DataSet.
    /// </summary>
    Task<IGenericResult<IReadOnlyList<DataSetFieldsPayload>>> GetDataSetFields(CancellationToken ct = default);

    /// <summary>
    /// Gets the catalogue of built-in and user-defined functions available in formula expressions.
    /// </summary>
    Task<IGenericResult<IReadOnlyList<FunctionInfo>>> GetFunctions(CancellationToken ct = default);

    /// <summary>
    /// Executes a windowed calculation.
    /// </summary>
    Task<IGenericResult<WindowedCalculationResponsePayload>> ExecuteWindowedCalculation(WindowedCalculationRequestPayload request, CancellationToken ct = default);

    // ── Calculation Entity (structural CRUD) ────────────────────────────────────

    /// <summary>
    /// Gets all calculation entities as a summary list.
    /// Use to look up an entity by name before creating or updating.
    /// </summary>
    Task<IGenericResult<IReadOnlyList<CalculationEntitySummaryModel>>> GetCalculationEntities(CancellationToken ct = default);

    /// <summary>
    /// Gets a calculation entity detail by its identifier.
    /// </summary>
    Task<IGenericResult<CalculationEntityDetailModel>> GetCalculationEntity(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Creates a new calculation entity (header + inputs + output spec).
    /// </summary>
    Task<IGenericResult<CalculationEntityDetailModel>> CreateCalculationEntity(CreateCalculationEntityModel request, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing calculation entity (version-on-write).
    /// </summary>
    Task<IGenericResult<CalculationEntityDetailModel>> UpdateCalculationEntity(Guid id, UpdateCalculationEntityModel request, CancellationToken ct = default);
}
