using Fdw.Data.DataSets.Abstractions;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// A Graph data set draft and what checking it found (FDW-5454).
/// </summary>
/// <param name="Draft">The draft read from the body of a create request, as saving would build it.</param>
/// <param name="Payload">Every problem and every intended source not bound yet.</param>
internal sealed record GraphDataSetDraftCheck(GraphDataSetConfiguration Draft, GraphDataSetValidationPayload Payload);
