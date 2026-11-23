using System;
using System.Collections.Generic;
using System.Text.Json;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Graphs;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>Base class for the readers of the kinds of setting a graph request states.</summary>
public abstract class SettingReaderBase : TypeOptionBase<SettingReaderBase>, ISettingReader
{
    /// <summary>Initializes a new instance of the <see cref="SettingReaderBase"/> class.</summary>
    /// <param name="name">The option name.</param>
    /// <param name="kind">The kind of setting this reader reads.</param>
    protected SettingReaderBase(string name, string kind)
        : base(name)
    {
        Kind = kind;
    }

    /// <inheritdoc />
    [TypeLookup("ByKind")]
    public string Kind { get; }

    /// <inheritdoc />
    public abstract IGenericResult Read(PipelineComponentConfiguration component, StageMember setting, JsonElement value, IReadOnlyDictionary<string, Guid> transformationIds);
}
