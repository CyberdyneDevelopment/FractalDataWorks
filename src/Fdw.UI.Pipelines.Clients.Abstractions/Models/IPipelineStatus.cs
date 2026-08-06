using System.Text.Json.Serialization;
using Fdw.Collections;
using Fdw.UI.Pipelines.Clients.Models.Converters;

namespace Fdw.UI.Pipelines.Clients.Models;

/// <summary>
/// Interface for pipeline lifecycle statuses.
/// </summary>
// Why: applying the converter on the interface makes every IPipelineStatus-typed property
// (de)serialize correctly wherever it appears, on the wire or on disk, without per-call wiring.
[JsonConverter(typeof(PipelineStatusJsonConverter))]
public interface IPipelineStatus : ITypeOption<int, PipelineStatusBase> { }
