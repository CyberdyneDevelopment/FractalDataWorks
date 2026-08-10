using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.UI.Pipelines.Clients.Models;

namespace Fdw.UI.Pipelines.Clients;

/// <summary>
/// Defines the contract for the pipeline designer API — task type discovery and step type discovery.
/// </summary>
/// <remarks>
/// Store-CRUD operations (Get/Create/Update/Validate/Delete) were backed by the retired
/// <c>FileSystemDesignerPipelineStore</c>. Load and save of real pipelines go through
/// <see cref="Fdw.Services.Pipelines.Clients.Abstractions.IPipelineClient"/>.
/// </remarks>
public interface IPipelineDesignerClient
{
    /// <summary>
    /// Gets all task types available in the designer palette.
    /// </summary>
    Task<IGenericResult<IReadOnlyList<TaskTypeInfo>>> GetTaskTypes(CancellationToken ct = default);

    /// <summary>
    /// Gets all pipeline step types registered in the <c>PipelineStepTypes</c> TypeCollection.
    /// </summary>
    Task<IGenericResult<IReadOnlyList<PipelineStepTypeSummary>>> GetStepTypes(CancellationToken ct = default);
}
