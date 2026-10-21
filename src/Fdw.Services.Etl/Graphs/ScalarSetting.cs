using System;

namespace Fdw.Services.Etl.Graphs;

/// <summary>
/// A scalar setting of a component kind and where a stored component keeps it (FDW-1396).
/// </summary>
/// <param name="Name">The setting's name in the component catalogue.</param>
/// <param name="Kind">What the setting holds: <c>Text</c>, <c>Number</c> or <c>Boolean</c>.</param>
/// <param name="Get">Reads the setting from a stored component; <see langword="null"/> when it is not stated.</param>
/// <param name="Set">Writes the setting to a stored component; <see langword="null"/> states nothing.</param>
public sealed record ScalarSetting(
    string Name,
    string Kind,
    Func<PipelineComponentConfiguration, object?> Get,
    Action<PipelineComponentConfiguration, object?> Set);
