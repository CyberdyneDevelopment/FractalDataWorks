using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Graphs;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>The parameters a component takes.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(SettingReaders), "Parameters")]
public sealed class ParametersSettingReader : SettingReaderBase
{
    /// <summary>Initializes a new instance of the <see cref="ParametersSettingReader"/> class.</summary>
    public ParametersSettingReader()
        : base("Parameters", "Parameters")
    {
    }

    /// <inheritdoc />
    public override IGenericResult Read(PipelineComponentConfiguration component, StageMember setting, JsonElement value, IReadOnlyDictionary<string, Guid> transformationIds)
        => GraphPayloadReader.ReadParameters(component, setting.Name, value);
}
