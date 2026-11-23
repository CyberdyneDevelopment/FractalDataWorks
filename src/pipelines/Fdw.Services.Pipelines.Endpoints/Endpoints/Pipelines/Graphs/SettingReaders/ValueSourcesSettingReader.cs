using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Graphs;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>The sources of the values of a load's columns.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(SettingReaders), "ValueSources")]
public sealed class ValueSourcesSettingReader : SettingReaderBase
{
    /// <summary>Initializes a new instance of the <see cref="ValueSourcesSettingReader"/> class.</summary>
    public ValueSourcesSettingReader()
        : base("ValueSources", "ValueSources")
    {
    }

    /// <inheritdoc />
    public override IGenericResult Read(PipelineComponentConfiguration component, StageMember setting, JsonElement value, IReadOnlyDictionary<string, Guid> transformationIds)
        => GraphPayloadReader.ReadValueSources(component, setting.Name, value);
}
