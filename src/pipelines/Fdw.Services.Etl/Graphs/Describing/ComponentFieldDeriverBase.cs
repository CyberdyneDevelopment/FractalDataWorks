using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Results;

namespace Fdw.Services.Etl.Graphs.Describing;

/// <summary>
/// Base class for the options of <see cref="ComponentFieldDerivers"/>, using the CRTP pattern.
/// </summary>
public abstract class ComponentFieldDeriverBase : TypeOptionBase<ComponentFieldDeriverBase>, IComponentFieldDeriver
{
    /// <summary>Initializes a new instance of the <see cref="ComponentFieldDeriverBase"/> class.</summary>
    /// <param name="name">The component kind this deriver describes.</param>
    protected ComponentFieldDeriverBase(string name) : base(name)
    {
    }

    /// <inheritdoc />
    public abstract Task<IGenericResult<IReadOnlyDictionary<string, IReadOnlyList<GraphField>?>>> Derive(
        PipelineComponentConfiguration component,
        IReadOnlyDictionary<string, IReadOnlyList<GraphField>?> inputs,
        DescribeContext context,
        CancellationToken cancellationToken = default);

    /// <summary>Makes the answer of a deriver whose outputs are all known now.</summary>
    /// <param name="outputs">The fields at each output node, by node name.</param>
    /// <returns>The successful answer.</returns>
    protected static Task<IGenericResult<IReadOnlyDictionary<string, IReadOnlyList<GraphField>?>>> Answer(
        IReadOnlyDictionary<string, IReadOnlyList<GraphField>?> outputs) =>
        Task.FromResult<IGenericResult<IReadOnlyDictionary<string, IReadOnlyList<GraphField>?>>>(
            GenericResult<IReadOnlyDictionary<string, IReadOnlyList<GraphField>?>>.Success(outputs));

    /// <summary>Makes a table of node names to fields that ignores case.</summary>
    /// <returns>An empty table.</returns>
    protected static Dictionary<string, IReadOnlyList<GraphField>?> NewTable() =>
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Finds the type of a field among fields that may not be known.</summary>
    /// <param name="fields">The fields, or <see langword="null"/> when they cannot be known.</param>
    /// <param name="name">The field's name.</param>
    /// <returns>The field's type, or <see langword="null"/> when it is not known.</returns>
    protected static string? TypeOf(IReadOnlyList<GraphField>? fields, string name) =>
        fields?.FirstOrDefault(field => string.Equals(field.Name, name, StringComparison.OrdinalIgnoreCase))?.Type;

    /// <summary>Finds the origin of a field among fields that may not be known (FDW-2156).</summary>
    /// <param name="fields">The fields, or <see langword="null"/> when they cannot be known.</param>
    /// <param name="name">The field's name.</param>
    /// <returns>The field's origin, or <see langword="null"/> when it has none or it is not known.</returns>
    protected static GraphFieldOrigin? OriginOf(IReadOnlyList<GraphField>? fields, string name) =>
        fields?.FirstOrDefault(field => string.Equals(field.Name, name, StringComparison.OrdinalIgnoreCase))?.Origin;

    /// <summary>Reads a component's field mappings of one setting as fields, each typed by its constant or by the source field it takes.</summary>
    /// <param name="component">The stored component.</param>
    /// <param name="setting">The setting the mappings belong to.</param>
    /// <param name="inputName">The input node the mappings belong to, or <see langword="null"/>.</param>
    /// <param name="source">The fields the mappings read from, or <see langword="null"/> when they cannot be known.</param>
    /// <returns>The fields the mappings produce, in order.</returns>
    protected static IReadOnlyList<GraphField> Mapped(
        PipelineComponentConfiguration component, string setting, string? inputName, IReadOnlyList<GraphField>? source) =>
        InputMappings.Fields(
            component.Fields
                .Where(field => string.Equals(field.SettingName, setting, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(field.InputName, inputName, StringComparison.OrdinalIgnoreCase))
                .OrderBy(field => field.Ordinal),
            source);
}
