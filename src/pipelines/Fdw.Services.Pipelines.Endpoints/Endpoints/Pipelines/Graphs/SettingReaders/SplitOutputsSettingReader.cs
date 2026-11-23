using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Graphs;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>The outputs of a split.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(SettingReaders), "SplitOutputs")]
public sealed class SplitOutputsSettingReader : SettingReaderBase
{
    /// <summary>Initializes a new instance of the <see cref="SplitOutputsSettingReader"/> class.</summary>
    public SplitOutputsSettingReader()
        : base("SplitOutputs", "SplitOutputs")
    {
    }

    /// <inheritdoc />
    public override IGenericResult Read(PipelineComponentConfiguration component, StageMember setting, JsonElement value, IReadOnlyDictionary<string, Guid> transformationIds)
        => GraphPayloadReader.ReadSplitOutputs(component, setting.Name, value);
}
