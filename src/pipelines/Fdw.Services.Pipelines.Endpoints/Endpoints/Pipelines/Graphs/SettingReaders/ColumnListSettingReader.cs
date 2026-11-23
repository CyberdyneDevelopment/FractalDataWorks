using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Graphs;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>A list of columns.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(SettingReaders), "ColumnList")]
public sealed class ColumnListSettingReader : SettingReaderBase
{
    /// <summary>Initializes a new instance of the <see cref="ColumnListSettingReader"/> class.</summary>
    public ColumnListSettingReader()
        : base("ColumnList", "ColumnList")
    {
    }

    /// <inheritdoc />
    public override IGenericResult Read(PipelineComponentConfiguration component, StageMember setting, JsonElement value, IReadOnlyDictionary<string, Guid> transformationIds)
        => GraphPayloadReader.ReadColumns(component, setting.Name, value);
}
