namespace Fdw.Web.Calculations.Clients.ApiClients;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Web.Calculations.Clients.CalculationEntities;
using Fdw.Web.Calculations.Clients.Formula;
using Fdw.Web.Calculations.Clients.Models;
using Fdw.Web.Clients.Abstractions;
using Microsoft.Extensions.Logging;

/// <summary>
/// API client for calculation management and execution endpoints.
/// </summary>
public class CalculationApiClient : ApiClientBase, ICalculationApiClient
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CalculationApiClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client configured for the calculations API.</param>
    /// <param name="logger">The logger instance.</param>
    public CalculationApiClient(HttpClient httpClient, ILogger<CalculationApiClient> logger)
        : base(httpClient, logger)
    {
    }

    /// <summary>
    /// Gets all available calculation types.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the calculation types response.</returns>
    public virtual Task<IGenericResult<CalculationTypesResponse>> GetCalculationTypes(CancellationToken ct = default)
        => Get<CalculationTypesResponse>("calculations/types", ct);

    /// <summary>Gets the period comparison types a calculation can be evaluated across.</summary>
    /// <param name="ct">A token to cancel the request.</param>
    /// <returns>A result containing the available period comparison types.</returns>
    public virtual Task<IGenericResult<PeriodComparisonTypesResponse>> GetPeriodComparisonTypes(CancellationToken ct = default)
        => Get<PeriodComparisonTypesResponse>("calculations/period-comparisons", ct);

    /// <summary>
    /// Executes a calculation.
    /// </summary>
    /// <param name="request">The execution request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the calculation execution response.</returns>
    public virtual Task<IGenericResult<ExecuteCalculationResponse>> ExecuteCalculation(ExecuteCalculationRequest request, CancellationToken ct = default)
        => Post<ExecuteCalculationRequest, ExecuteCalculationResponse>("calculations/execute", request, ct);

    /// <summary>
    /// Gets all defined calculations.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the list of calculation summaries.</returns>
    public virtual Task<IGenericResult<IReadOnlyList<CalculationSummaryPayload>>> GetCalculations(CancellationToken ct = default)
        => GetList<CalculationSummaryPayload>("calculation-entities", ct);

    /// <summary>
    /// Gets a specific calculation definition by identifier.
    /// </summary>
    /// <param name="id">The unique identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the calculation details.</returns>
    public virtual Task<IGenericResult<CalculationDetailPayload>> GetCalculation(Guid id, CancellationToken ct = default)
        => Get<CalculationDetailPayload>($"calculation-entities/{id}", ct);

    /// <summary>
    /// Creates a new calculation definition.
    /// </summary>
    /// <param name="request">The create request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the created calculation details.</returns>
    public virtual Task<IGenericResult<CalculationDetailPayload>> CreateCalculation(CreateCalculationDefinitionRequest request, CancellationToken ct = default)
        => Post<CreateCalculationDefinitionRequest, CalculationDetailPayload>("calculation-entities", request, ct);

    /// <summary>
    /// Updates an existing calculation definition.
    /// </summary>
    /// <param name="id">The unique identifier.</param>
    /// <param name="request">The update request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the updated calculation details.</returns>
    public virtual Task<IGenericResult<CalculationDetailPayload>> UpdateCalculation(Guid id, UpdateCalculationDefinitionRequest request, CancellationToken ct = default)
        => Patch<UpdateCalculationDefinitionRequest, CalculationDetailPayload>($"calculation-entities/{id}", request, ct);

    /// <summary>
    /// Deletes a calculation definition.
    /// </summary>
    /// <param name="id">The unique identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result indicating success or failure of the deletion.</returns>
    public virtual Task<IGenericResult> DeleteCalculation(Guid id, CancellationToken ct = default)
        => Delete($"calculation-entities/{id}", ct);

    /// <summary>
    /// Validates a calculation formula.
    /// </summary>
    /// <param name="request">The validation request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the formula validation response.</returns>
    public virtual Task<IGenericResult<PreviewFormulaResponse>> ValidateFormula(ValidateFormulaPayload request, CancellationToken ct = default)
        // Why: server route is ValidateFormulaEndpointBase at "calculation-entities/validate-formula";
        // the client previously POSTed "calculations/validate" (404). Align to the server contract.
        => Post<ValidateFormulaPayload, PreviewFormulaResponse>("calculation-entities/validate-formula", request, ct);

    /// <summary>
    /// Previews a calculation with sample data.
    /// </summary>
    /// <param name="request">The preview request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the calculation preview response.</returns>
    public virtual Task<IGenericResult<PreviewCalculationResponse>> PreviewCalculation(PreviewCalculationRequest request, CancellationToken ct = default)
        => Post<PreviewCalculationRequest, PreviewCalculationResponse>("calculations/preview", request, ct);

    /// <summary>
    /// Gets the fields for a specific DataSet.
    /// </summary>
    /// <param name="dataSetName">The DataSet name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the list of DataSet fields.</returns>
    public virtual Task<IGenericResult<IReadOnlyList<DataSetFieldPayload>>> GetDataSetFields(string dataSetName, CancellationToken ct = default)
        => GetList<DataSetFieldPayload>($"datasets/{Uri.EscapeDataString(dataSetName)}/fields", ct);

    /// <summary>
    /// Gets fields for all DataSets in a single call.
    /// Avoids N+1 requests when the formula editor needs field lists from every DataSet.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing all DataSet field groups.</returns>
    public virtual Task<IGenericResult<IReadOnlyList<DataSetFieldsPayload>>> GetDataSetFields(CancellationToken ct = default)
        => GetList<DataSetFieldsPayload>("datasets/fields", ct);

    /// <summary>
    /// Gets the catalogue of built-in and user-defined functions available in formula expressions.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the list of available functions.</returns>
    public virtual Task<IGenericResult<IReadOnlyList<FunctionInfo>>> GetFunctions(CancellationToken ct = default)
        => GetList<FunctionInfo>("calculations/functions", ct);

    /// <summary>
    /// Executes a windowed calculation.
    /// </summary>
    /// <param name="request">The windowed calculation request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the windowed calculation response.</returns>
    public virtual Task<IGenericResult<WindowedCalculationResponsePayload>> ExecuteWindowedCalculation(WindowedCalculationRequestPayload request, CancellationToken ct = default)
        => Post<WindowedCalculationRequestPayload, WindowedCalculationResponsePayload>("calculations/windowed", request, ct);

    // ── Calculation Entity (structural CRUD) ────────────────────────────────────

    /// <summary>
    /// Gets all calculation entities as a summary list.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the list of calculation entity summaries.</returns>
    public virtual Task<IGenericResult<IReadOnlyList<CalculationEntitySummaryModel>>> GetCalculationEntities(CancellationToken ct = default)
        => GetList<CalculationEntitySummaryModel>("calculation-entities", ct);

    /// <summary>
    /// Gets a calculation entity detail by its identifier.
    /// </summary>
    /// <param name="id">The unique identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the calculation entity detail.</returns>
    public virtual Task<IGenericResult<CalculationEntityDetailModel>> GetCalculationEntity(Guid id, CancellationToken ct = default)
        => Get<CalculationEntityDetailModel>($"calculation-entities/{id}", ct);

    /// <summary>
    /// Creates a new calculation entity.
    /// </summary>
    /// <param name="request">The create request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the created calculation entity detail.</returns>
    public virtual Task<IGenericResult<CalculationEntityDetailModel>> CreateCalculationEntity(CreateCalculationEntityModel request, CancellationToken ct = default)
        => Post<CreateCalculationEntityModel, CalculationEntityDetailModel>("calculation-entities", request, ct);

    /// <summary>
    /// Updates an existing calculation entity.
    /// </summary>
    /// <param name="id">The unique identifier.</param>
    /// <param name="request">The update request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the updated calculation entity detail.</returns>
    public virtual Task<IGenericResult<CalculationEntityDetailModel>> UpdateCalculationEntity(Guid id, UpdateCalculationEntityModel request, CancellationToken ct = default)
        => Patch<UpdateCalculationEntityModel, CalculationEntityDetailModel>($"calculation-entities/{id}", request, ct);
}
