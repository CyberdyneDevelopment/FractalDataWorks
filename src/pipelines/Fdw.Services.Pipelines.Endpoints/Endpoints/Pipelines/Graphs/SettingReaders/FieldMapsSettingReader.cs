using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Graphs;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>The field maps of a component.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(SettingReaders), "FieldMaps")]
public sealed class FieldMapsSettingReader : SettingReaderBase
{
    /// <summary>Initializes a new instance of the <see cref="FieldMapsSettingReader"/> class.</summary>
    public FieldMapsSettingReader()
        : base("FieldMaps", "FieldMaps")
    {
    }

    /// <inheritdoc />
    public override IGenericResult Read(PipelineComponentConfiguration component, StageMember setting, JsonElement value, IReadOnlyDictionary<string, Guid> transformationIds)
        => GraphFieldMapReader.Read(component, setting, value, transformationIds);
}
