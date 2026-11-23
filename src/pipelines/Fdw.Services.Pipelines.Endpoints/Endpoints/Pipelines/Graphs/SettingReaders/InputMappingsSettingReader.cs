using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Graphs;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>The input mappings of a component, read as field maps are.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(SettingReaders), "InputMappings")]
public sealed class InputMappingsSettingReader : SettingReaderBase
{
    /// <summary>Initializes a new instance of the <see cref="InputMappingsSettingReader"/> class.</summary>
    public InputMappingsSettingReader()
        : base("InputMappings", ComponentTypeBase.MappingsKind)
    {
    }

    /// <inheritdoc />
    public override IGenericResult Read(PipelineComponentConfiguration component, StageMember setting, JsonElement value, IReadOnlyDictionary<string, Guid> transformationIds)
        => GraphFieldMapReader.Read(component, setting, value, transformationIds);
}
