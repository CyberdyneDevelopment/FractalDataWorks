using System.Text.Json.Serialization;
using Fdw.Collections;
using Fdw.UI.Pipelines.Clients.Models.Converters;

namespace Fdw.UI.Pipelines.Clients.Models;

/// <summary>
/// Interface for pipeline lifecycle statuses.
/// </summary>
[JsonConverter(typeof(PipelineStatusJsonConverter))]
public interface IPipelineStatus : ITypeOption<int, PipelineStatusBase> { }
