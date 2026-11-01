using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Fdw.Conventions;
using Fdw.Results;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Graphs.Tools;
using Fdw.Services.Etl.Transforms;
using Fdw.Services.Pipelines.Clients.Abstractions;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Reads a graph the API states into the stored configuration (FDW-1396).
/// </summary>
/// <remarks>
/// It reads settings by what the component catalogue says each setting is, so a kind the catalogue lists is readable with no
/// change here. It refuses what it cannot place: a kind that is not in the catalogue, a setting the kind does not list, or a
/// value of the wrong JSON type. It states nothing for a setting the request leaves out; whether a missing one is required is
/// the graph check's to say, so there is one place that says it.
/// </remarks>
public static class GraphPayloadReader
{
    /// <summary>The implementation name a graph pipeline is stored under.</summary>
    public const string Implementation = "Graph";

    /// <summary>Reads a graph that is to be saved, whose field mappings name no transformation.</summary>
    /// <param name="payload">What the API states.</param>
    /// <param name="id">The pipeline's durable id.</param>
    /// <returns>The stored configuration, or the refusal.</returns>
    public static IGenericResult<GraphPipelineConfiguration> Read(GraphPayload payload, Guid id) =>
        Read(payload, id, StageChains.None.Ids);

    /// <summary>Reads a graph that is to be saved.</summary>
    /// <param name="payload">What the API states.</param>
    /// <param name="id">The pipeline's durable id.</param>
    /// <param name="transformationIds">The id of each transformation the field mappings of its tools name, by name (see <see cref="GraphChainLookup"/>).</param>
    /// <returns>The stored configuration, or the refusal.</returns>
    public static IGenericResult<GraphPipelineConfiguration> Read(GraphPayload payload, Guid id, IReadOnlyDictionary<string, Guid> transformationIds)
    {
        ArgumentNullException.ThrowIfNull(payload);
        if (payload.IsEnabled is null)
        {
            return GraphRefusal.Of<GraphPipelineConfiguration>("the graph does not state isEnabled");
        }

        if (payload.LinkCapacity is null)
        {
            return GraphRefusal.Of<GraphPipelineConfiguration>("the graph does not state linkCapacity");
        }

        if (payload.MaxParallelRuns is null)
        {
            return GraphRefusal.Of<GraphPipelineConfiguration>("the graph does not state maxParallelRuns");
        }

        if (string.IsNullOrWhiteSpace(payload.OverflowBehavior))
        {
            return GraphRefusal.Of<GraphPipelineConfiguration>("the graph does not state overflowBehavior");
        }

        return Place(payload, id, null, transformationIds);
    }

    /// <summary>
    /// Reads a graph that is only being checked. What a save requires and a check lists as a problem is left unstated, not filled
    /// in: a missing link capacity reads as zero, which the check reports, and the enabled flag, which the check does not judge,
    /// reads as off.
    /// </summary>
    /// <remarks>
    /// Why a draft collects what it cannot read instead of refusing the first thing: a person fills a pipeline in a setting at a time,
    /// and a half-finished setting (a sort field row with no field) must not hide the fields every other port can still show. What
    /// cannot be read is listed as a problem at its place and left out of the draft. A save still refuses the first such thing.
    /// </remarks>
    /// <param name="payload">What the API states.</param>
    /// <param name="id">The pipeline's durable id.</param>
    /// <param name="problems">Receives each thing the draft states that could not be read, with where it is.</param>
    /// <returns>The stored configuration, or the refusal when nothing of the draft can be placed.</returns>
    public static IGenericResult<GraphPipelineConfiguration> ReadDraft(GraphPayload payload, Guid id, List<GraphProblem> problems) =>
        ReadDraft(payload, id, problems, StageChains.None.Ids);

    /// <summary>Reads a graph that is only being checked, resolving the transformations its field mappings name.</summary>
    /// <param name="payload">What the API states.</param>
    /// <param name="id">The pipeline's durable id.</param>
    /// <param name="problems">Receives each thing the draft states that could not be read, with where it is.</param>
    /// <param name="transformationIds">The id of each transformation the field mappings of its tools name, by name (see <see cref="GraphChainLookup"/>).</param>
    /// <returns>The stored configuration, or the refusal when nothing of the draft can be placed.</returns>
    public static IGenericResult<GraphPipelineConfiguration> ReadDraft(
        GraphPayload payload, Guid id, List<GraphProblem> problems, IReadOnlyDictionary<string, Guid> transformationIds)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentNullException.ThrowIfNull(problems);
        return Place(payload, id, problems, transformationIds);
    }

    [ConventionOverride(MaxCyclomaticComplexity = 21)]
    private static IGenericResult<GraphPipelineConfiguration> Place(
        GraphPayload payload, Guid id, List<GraphProblem>? problems, IReadOnlyDictionary<string, Guid> transformationIds)
    {
        var named = !string.IsNullOrWhiteSpace(payload.Name);
        if (!named && problems is null)
        {
            return GraphRefusal.Of<GraphPipelineConfiguration>("the graph has no name");
        }

        if (!named)
        {
            problems?.Add(new GraphProblem(["name"], "the graph has no name"));
        }

        var graph = new GraphPipelineConfiguration
        {
            Id = id,
            Name = named ? payload.Name : string.Empty,
            Implementation = Implementation,
            IsEnabled = payload.IsEnabled == true,
            Description = payload.Description,
            LinkCapacity = payload.LinkCapacity is { } capacity ? capacity : 0,
            RequireApprovalToRun = payload.RequireApprovalToRun,
            MaxParallelRuns = payload.MaxParallelRuns is { } parallel ? parallel : 0,
            OverflowBehavior = payload.OverflowBehavior is { } overflow ? overflow : string.Empty,
            DelayedStartSeconds = payload.DelayedStartSeconds,
            DelayedStartTimeOfDay = payload.DelayedStartTimeOfDay,
            DelayedStartTimeZoneId = payload.DelayedStartTimeZoneId,
        };

        foreach (var variable in payload.Variables)
        {
            graph.Variables.Add(new PipelineVariableConfiguration { Name = variable.Name, DataType = variable.DataType, ValueText = variable.Value });
        }

        foreach (var parameter in payload.Parameters)
        {
            graph.Parameters.Add(new PipelineParameterConfiguration
            {
                Name = parameter.Name, ParameterType = parameter.DataType, IsRequired = parameter.IsRequired, IsReadOnly = parameter.IsReadOnly,
                IsList = parameter.IsList, ShowsValue = parameter.ShowsValue, DefaultText = parameter.DefaultValue, Description = parameter.Description,
            });
        }

        // The order the components run in is the stage each states, so the place in the list means nothing.
        foreach (var component in payload.Components)
        {
            var read = ReadComponent(component, problems, transformationIds);
            if (read.IsFailure || read.Value is null)
            {
                return read.ToNewResult<GraphPipelineConfiguration>();
            }

            graph.Components.Add(read.Value);
        }

        foreach (var link in payload.Links)
        {
            graph.Links.Add(new PipelineLinkConfiguration
            {
                FromComponent = link.From.Component,
                FromOutput = link.From.Node,
                ToComponent = link.To.Component,
                ToInput = link.To.Node,
            });
        }

        foreach (var dropped in payload.Dropped)
        {
            graph.DroppedOutputs.Add(new PipelineDroppedOutputConfiguration { ComponentName = dropped.Component, OutputName = dropped.Node });
        }

        return GenericResult<GraphPipelineConfiguration>.Success(graph);
    }

    internal static IGenericResult<PipelineComponentConfiguration> ReadComponent(
        GraphComponentPayload payload, List<GraphProblem>? problems, IReadOnlyDictionary<string, Guid> transformationIds)
    {
        var kind = ComponentTypes.ByName(payload.Kind);
        if (ReferenceEquals(kind, ComponentTypes.NotFound))
        {
            return GraphRefusal.Of<PipelineComponentConfiguration>(
                $"component '{payload.Name}' is of kind '{payload.Kind}', which is not a component kind");
        }

        var component = new PipelineComponentConfiguration
        {
            Name = payload.Name,
            ComponentType = kind.Name,
            ParentComponent = payload.Parent,
            LayoutX = payload.Layout?.X,
            LayoutY = payload.Layout?.Y,
        };

        var toolMembers = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, raw) in payload.Settings)
        {
            var read = ReadNamedSetting(payload.Name, kind, component, toolMembers, (name, raw), problems, transformationIds);
            if (read.IsFailure)
            {
                return read.ToNewResult<PipelineComponentConfiguration>();
            }
        }

        var staged = ReadStage(payload.Name, kind, component, toolMembers, problems, transformationIds);
        return staged.IsFailure
            ? staged.ToNewResult<PipelineComponentConfiguration>()
            : GenericResult<PipelineComponentConfiguration>.Success(component);
    }

    // One setting of a component. A setting that cannot be read is a refusal of the save; in a draft it is a problem listed and the
    // read goes on, so the rest of the component is still described.
    private static IGenericResult ReadNamedSetting(
        string componentName,
        IComponentType kind,
        PipelineComponentConfiguration component,
        Dictionary<string, JsonElement> toolMembers,
        (string Name, object? Value) stated,
        List<GraphProblem>? problems,
        IReadOnlyDictionary<string, Guid> transformationIds)
    {
        var value = stated.Value switch
        {
            JsonElement element => element,
            null => default,
            _ => JsonSerializer.SerializeToElement(stated.Value, JsonOptions),
        };
        var setting = kind.Settings.FirstOrDefault(s => string.Equals(s.Name, stated.Name, StringComparison.OrdinalIgnoreCase));
        if (setting is null)
        {
            var notTaken = $"component '{componentName}' of kind '{kind.Name}' states the setting '{stated.Name}', which the kind does not take";
            return Noted(problems, GraphRefusal.Of<bool>(notTaken), ["components", componentName, "settings", stated.Name], notTaken);
        }

        var absent = value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined;

        // A tool's stage members are not stored one by one: they make up the one stage the tool holds, read after the settings.
        if (kind is StageToolComponentTypeBase tool && tool.MemberNames.Contains(setting.Name, StringComparer.OrdinalIgnoreCase))
        {
            if (!absent)
            {
                var bound = BoundStageList(component, setting, value);
                toolMembers[setting.Name] = bound.HasValue ? bound.Value : value;
            }

            return GenericResult.Success();
        }

        var read = absent ? GenericResult.Success() : ReadSetting(component, setting, value, transformationIds);
        return read.IsSuccess
            ? read
            : Noted(
                problems,
                GraphRefusal.From<bool>(read),
                ["components", componentName, "settings", setting.Name],
                PipelineProblems.ReasonOf(read.CurrentMessage, "reading the setting"));
    }

    // A stage member that is a list a run supplies (FDW-1745): { "parameter": "Name" } is kept as a binding on the component, and the stage is
    // read with a one-item placeholder in its place so the kind's own checks pass; the placeholder is cleared from the stage once it is read.
    private static JsonElement? BoundStageList(PipelineComponentConfiguration component, StageMember setting, JsonElement value)
    {
        if (!SettingBindingRules.IsStageListKind(setting.Kind) || GraphSettingValue.ParameterOf(value) is not { } parameter)
        {
            return null;
        }

        component.SettingBindings.Add(new PipelineComponentSettingBindingConfiguration { SettingName = setting.Name, ParameterName = parameter });
        return StageListPlaceholder(setting.Kind);
    }

    // What a stage kind's own checks accept in place of the list: a key column for a Distinct, a field and a direction for a Sort.
    private static JsonElement StageListPlaceholder(string settingKind) =>
        string.Equals(settingKind, "SortFieldList", StringComparison.Ordinal)
            ? JsonSerializer.SerializeToElement(new[] { new { field = PlaceholderColumn, direction = "Ascending" } }, JsonOptions)
            : JsonSerializer.SerializeToElement(new { keyColumns = new[] { PlaceholderColumn } }, JsonOptions);

    // Empties the stage lists whose columns come from a parameter, so the stored stage holds none of the placeholder.
    private static void ClearBoundStageLists(PipelineComponentConfiguration component, StageToolComponentTypeBase tool, PipelineTransformConfiguration stage)
    {
        foreach (var binding in component.SettingBindings)
        {
            if (SettingBindingRules.ListSetting(tool, binding.SettingName) is not { } list || !SettingBindingRules.IsStageListKind(list.Kind))
            {
                continue;
            }

            if (string.Equals(list.Kind, "SortFieldList", StringComparison.Ordinal))
            {
                stage.SortFields = [];
            }
            else
            {
                stage.GroupByFields = [];
            }
        }
    }

    // The one stage a tool holds, read from the members the tool states.
    private static IGenericResult ReadStage(
        string componentName,
        IComponentType kind,
        PipelineComponentConfiguration component,
        Dictionary<string, JsonElement> toolMembers,
        List<GraphProblem>? problems,
        IReadOnlyDictionary<string, Guid> transformationIds)
    {
        if (kind is not StageToolComponentTypeBase stageTool || toolMembers.Count == 0)
        {
            return GenericResult.Success();
        }

        var staged = ReadToolStage(component, stageTool, toolMembers, transformationIds);
        return staged.IsSuccess
            ? staged
            : Noted(
                problems,
                GraphRefusal.From<bool>(staged),
                ToolPath(componentName, toolMembers),
                // Why the setting is named only when the tool states one member: a refused stage is refused as a whole, and the one
                // member stated is the one it is about.
                PipelineProblems.ReasonOf(staged.CurrentMessage, "reading the stage"));
    }

    // A save is refused at the first problem. A draft lists it and goes on.
    private static IGenericResult Noted(List<GraphProblem>? problems, IGenericResult refusal, string[] path, string reason)
    {
        if (problems is null)
        {
            return refusal;
        }

        problems.Add(new GraphProblem(path, reason));
        return GenericResult.Success();
    }

    private static string[] ToolPath(string component, Dictionary<string, JsonElement> members) =>
        members.Count == 1 ? ["components", component, "settings", members.Keys.First()] : ["components", component, "settings"];

    private static IGenericResult ReadSetting(
        PipelineComponentConfiguration component, StageMember setting, JsonElement value, IReadOnlyDictionary<string, Guid> transformationIds) =>
        setting.Kind switch
        {
            "ColumnList" => ReadColumns(component, setting.Name, value),
            "FieldMaps" or ComponentTypeBase.MappingsKind => GraphFieldMapReader.Read(component, setting, value, transformationIds),
            "SplitOutputs" => ReadSplitOutputs(component, setting.Name, value),
            "ValueSources" => ReadValueSources(component, setting.Name, value),
            "Parameters" => ReadParameters(component, setting.Name, value),
            _ => ReadScalar(component, setting.Name, value),
        };

    private static IGenericResult ReadScalar(PipelineComponentConfiguration component, string name, JsonElement value)
    {
        var binding = ComponentSettingBindings.Find(name);
        if (binding is null)
        {
            return Refused(component, name, "has no place to be stored");
        }

        if (GraphSettingValue.ParameterOf(value) is { } parameter)
        {
            component.SettingBindings.Add(new PipelineComponentSettingBindingConfiguration { SettingName = binding.Name, ParameterName = parameter });
            return GenericResult.Success();
        }

        var typed = GraphSettingValue.Read(binding.Kind, value);
        if (typed is null)
        {
            return Refused(component, name, $"must be {GraphSettingValue.Article(binding.Kind)}, or {{ \"parameter\": \"Name\" }} to take it from a run parameter");
        }

        binding.Set(component, typed);
        return GenericResult.Success();
    }

    // The values an Execute pipeline, Execute SQL or Set variable component gives its parameters: a list of a name and a value as text.
    private static IGenericResult ReadParameters(PipelineComponentConfiguration component, string name, JsonElement value)
    {
        var parameters = value.ValueKind == JsonValueKind.Array
            ? JsonSerializer.Deserialize<List<GraphComponentParameterPayload>>(value.GetRawText(), JsonOptions)
            : null;
        if (parameters is null)
        {
            return Refused(component, name, "must be a list of parameters, each with a name and a value");
        }

        foreach (var parameter in parameters)
        {
            component.Parameters.Add(new PipelineComponentParameterConfiguration { ParameterName = parameter.Name, ValueText = parameter.Value });
        }

        return GenericResult.Success();
    }

    private static IGenericResult ReadColumns(PipelineComponentConfiguration component, string name, JsonElement value)
    {
        if (GraphSettingValue.ParameterOf(value) is { } parameter)
        {
            component.SettingBindings.Add(new PipelineComponentSettingBindingConfiguration { SettingName = name, ParameterName = parameter });
            return GenericResult.Success();
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            return Refused(component, name, "must be a list of column names, or { \"parameter\": \"Name\" } to take the list from a list run parameter");
        }

        var ordinal = 0;
        foreach (var item in value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String || item.GetString() is not { Length: > 0 } column || string.IsNullOrWhiteSpace(column))
            {
                return Refused(component, name, "must be a list of column names");
            }

            component.Columns.Add(new PipelineComponentColumnConfiguration
            {
                SettingName = name, Ordinal = ordinal++, ColumnName = column,
            });
        }

        return GenericResult.Success();
    }

    private static IGenericResult ReadSplitOutputs(PipelineComponentConfiguration component, string name, JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Array)
        {
            return Refused(component, name, "must be a list of outputs, each with a name and a filter");
        }

        var outputs = JsonSerializer.Deserialize<List<GraphSplitOutputPayload>>(value.GetRawText(), JsonOptions);
        if (outputs is null)
        {
            return Refused(component, name, "must be a list of outputs, each with a name and a filter");
        }

        for (var ordinal = 0; ordinal < outputs.Count; ordinal++)
        {
            component.SplitOutputs.Add(new PipelineComponentSplitOutputConfiguration
            {
                Ordinal = ordinal, OutputName = outputs[ordinal].Name, FilterExpression = outputs[ordinal].Filter,
            });
        }

        return GenericResult.Success();
    }

    // The value sources are stated as the load step states them (a target column and its source), so a destination takes what the load
    // step took. A column with no source is refused here, naming the column.
    private static IGenericResult ReadValueSources(PipelineComponentConfiguration component, string name, JsonElement value)
    {
        const string shape = "must be a list of target columns, each with the source of its value";
        if (value.ValueKind != JsonValueKind.Array)
        {
            return Refused(component, name, shape);
        }

        var sources = JsonSerializer.Deserialize<List<PipelineLoadColumnSource>>(value.GetRawText(), JsonOptions);
        if (sources is null)
        {
            return Refused(component, name, shape);
        }

        for (var ordinal = 0; ordinal < sources.Count; ordinal++)
        {
            var entry = sources[ordinal];
            if (entry?.Source is not { } source)
            {
                return Refused(component, name, $"gives the column '{entry?.Column}' no source for its value");
            }

            component.ValueSources.Add(ValueSourceOf(ordinal, entry.Column, source));
        }

        return GenericResult.Success();
    }

    private static PipelineComponentValueSourceConfiguration ValueSourceOf(int ordinal, string column, PipelineLoadValueSource source) =>
        new()
        {
            Ordinal = ordinal,
            ColumnName = column,
            SourceKind = source.Kind,
            SourceField = source.SourceField,
            SequenceName = source.SequenceName,
            SequenceSchema = source.Schema,
            SequenceDatabase = source.Database,
            ConstantValue = source.Value,
            ConstantDataType = source.ValueType,
            OrderColumns =
            [
                .. source.OrderBy.Select((orderColumn, order) => new PipelineComponentValueSourceColumnConfiguration { Ordinal = order, ColumnName = orderColumn }),
            ],
        };

    // A tool holds one stage of the kind it is. The stage is stated as a stage of a pipeline is (a name, an operation type, the members
    // that kind takes) and is mapped by the one mapper a pipeline's stages go through, as a stage of a pipeline of type Graph, so a
    // kind that does not run on a graph is refused there with the message every refused stage gets.
    private static IGenericResult ReadToolStage(
        PipelineComponentConfiguration component, StageToolComponentTypeBase tool, Dictionary<string, JsonElement> members, IReadOnlyDictionary<string, Guid> transformationIds)
    {
        var stated = new JsonObject
        {
            ["name"] = component.Name,
            ["operationType"] = tool.StageKind,
        };
        foreach (var (name, value) in members)
        {
            stated[name] = JsonNode.Parse(value.GetRawText());
        }

        var request = stated.Deserialize<CreatePipelineTransformRequest>(JsonOptions);
        if (request is null)
        {
            return Refused(component, tool.StageKind, "is not a stage this kind can read");
        }

        var mapped = PipelineTransformConfigurationMapper.Map([request], NullLogger.Instance, transformationIds);
        if (mapped.IsFailure || mapped.Value is null)
        {
            return Refused(component, tool.StageKind, PipelineProblems.ReasonOf(mapped.Details?.GetValue<string>("Reason") is { Length: > 0 } reason ? reason : mapped.CurrentMessage, "reading the stage"));
        }

        foreach (var stage in mapped.Value)
        {
            ClearBoundStageLists(component, tool, stage);
            component.Stages.Add(stage);
        }

        return GenericResult.Success();
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    // The one column a bound stage list is read with, so the stage kind's own checks pass; it is cleared from the stage and never stored.
    private const string PlaceholderColumn = "bound-by-parameter";

    private static IGenericResult Refused(PipelineComponentConfiguration component, string setting, string reason) =>
        GraphRefusal.Of<bool>($"component '{component.Name}' setting '{setting}' {reason}");
}
