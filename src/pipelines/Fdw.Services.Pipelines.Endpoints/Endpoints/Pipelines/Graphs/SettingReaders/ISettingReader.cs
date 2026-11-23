using System;
using System.Collections.Generic;
using System.Text.Json;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Graphs;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>How a setting of one kind is read out of the JSON a graph request states, into the component it belongs to.</summary>
public interface ISettingReader : ITypeOption<int, SettingReaderBase>
{
    /// <summary>Gets the kind of setting this reader reads (what the setting's <c>Kind</c> says).</summary>
    string Kind { get; }

    /// <summary>Reads the setting into the component.</summary>
    /// <param name="component">The component the setting belongs to.</param>
    /// <param name="setting">The setting, of this reader's kind.</param>
    /// <param name="value">The JSON the request states for it.</param>
    /// <param name="transformationIds">The ids of the transformations the graph names, by name.</param>
    /// <returns>Success, or the refusal that says what is wrong with the value.</returns>
    IGenericResult Read(PipelineComponentConfiguration component, StageMember setting, JsonElement value, IReadOnlyDictionary<string, Guid> transformationIds);
}
