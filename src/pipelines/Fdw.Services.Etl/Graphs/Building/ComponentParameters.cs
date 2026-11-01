using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Graphs.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Graphs.Building;

/// <summary>
/// Renders the run parameters into the settings of a source or destination when the run starts (FDW-1689): <c>{{File}}</c> in the
/// container of a source becomes the value the run was given for <c>File</c>.
/// </summary>
/// <remarks>
/// The stored component is never changed: the configuration is shared and cached, so the run builds from a copy. A parameter the run was
/// started without a value for fails the run before any row is read, naming the component, the setting and the parameter; nothing is
/// substituted for it.
/// </remarks>
public static partial class ComponentParameters
{
    [GeneratedRegex(@"\{\{\s*(?<name>[^{}\s]+)\s*\}\}", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex Reference();

    /// <summary>Renders the parameters into a component's settings.</summary>
    /// <param name="component">The stored component.</param>
    /// <param name="context">What the run builds with; its parameters are the run's typed values.</param>
    /// <returns>The component to build from (the stored one when it holds no parameter, otherwise a copy), or the failure that names the missing parameter.</returns>
    public static IGenericResult<PipelineComponentConfiguration> Render(PipelineComponentConfiguration component, FlowBuildContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return Render(component, context.Parameters, context.Logger);
    }

    /// <summary>Renders the parameters into a component's settings from the run's typed values, without a build context (FDW-1891).</summary>
    /// <remarks>
    /// Why this reads no build context: the rendered component is what a run reads and writes, so the run's roles are judged against it before
    /// the run is queued, when only the values and a logger exist. The run builds from the same rendering.
    /// </remarks>
    /// <param name="component">The stored component.</param>
    /// <param name="parameters">The run's typed values by parameter name.</param>
    /// <param name="logger">The logger the failures are written to.</param>
    /// <returns>The component to build from (the stored one when it holds no parameter, otherwise a copy), or the failure that names the missing parameter.</returns>
    public static IGenericResult<PipelineComponentConfiguration> Render(
        PipelineComponentConfiguration component, IReadOnlyDictionary<string, object?> parameters, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(component);
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(logger);
        var interpolated = Interpolate(component, parameters, logger);
        if (interpolated.IsFailure || interpolated.Value is null)
        {
            return interpolated;
        }

        // Why the bindings after the text: a bound setting holds no text of its own, and a value a run supplies must not be read again as a template.
        return BoundSettings.Apply(interpolated.Value, parameters, logger);
    }

    // The settings that name what a source or destination reads or writes may hold {{Name}} amid other text (FDW-1689).
    private static IGenericResult<PipelineComponentConfiguration> Interpolate(
        PipelineComponentConfiguration component, IReadOnlyDictionary<string, object?> parameters, ILogger logger) =>
        Apply(component, ParameterSettings.Of(component), parameters, logger);

    /// <summary>
    /// Renders the settings of a component that are read when its step runs (FDW-1958), from the values then: the run's parameters and the
    /// variables as the steps before it left them. The stored component is not changed.
    /// </summary>
    /// <param name="component">The stored component, with its settings that are read when the run starts already rendered.</param>
    /// <param name="values">The parameters and variables as they are when the step runs.</param>
    /// <param name="logger">The logger a missing value is written to.</param>
    /// <returns>The component with those settings rendered, or the failure naming the component, the setting and the name that has no value.</returns>
    public static IGenericResult<PipelineComponentConfiguration> RenderLate(
        PipelineComponentConfiguration component, IReadOnlyDictionary<string, object?> values, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(component);
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(logger);
        var rendered = Apply(component, ParameterSettings.LateOf(component), values, logger);
        if (rendered.IsFailure || rendered.Value is null)
        {
            return rendered;
        }

        // A mapping's constant may name a value too (FDW-2119): it is read as the type the mapping states, as a script parameter is.
        rendered = RenderMappings(rendered.Value, values, logger);
        if (rendered.IsFailure || rendered.Value is null)
        {
            return rendered;
        }

        // A script's tokens, $(Name), are names read when the step runs too (FDW-1970); the copy is the one whose script is changed.
        if (!ScriptTokens.Holds(rendered.Value.ScriptText))
        {
            return rendered;
        }

        var script = ScriptTokens.Render(component.Name, rendered.Value.ScriptText!, values, logger);
        if (script.IsFailure || script.Value is null)
        {
            return script.ToNewResult<PipelineComponentConfiguration>();
        }

        var copy = rendered.Value.Copy();
        copy.ScriptText = script.Value;
        return GenericResult<PipelineComponentConfiguration>.Success(copy);
    }

    // The mapping rows whose constant names a value, rendered into copies; the stored rows are not changed.
    private static IGenericResult<PipelineComponentConfiguration> RenderMappings(
        PipelineComponentConfiguration component, IReadOnlyDictionary<string, object?> values, ILogger logger)
    {
        var named = ParameterSettings.MappingConstants(component);
        if (named.Count == 0)
        {
            return GenericResult<PipelineComponentConfiguration>.Success(component);
        }

        var rows = new List<PipelineComponentFieldConfiguration>(component.Fields.Count);
        foreach (var field in component.Fields)
        {
            if (!named.Contains(field))
            {
                rows.Add(field);
                continue;
            }

            var rendered = Text(component.Name, $"{ComponentTypeBase.MappingsSetting}.{field.TargetField}", field.ConstantText!, values, logger);
            if (rendered.IsFailure || rendered.Value is null)
            {
                return rendered.ToNewResult<PipelineComponentConfiguration>();
            }

            var row = field.Copy();
            row.ConstantText = rendered.Value;
            rows.Add(row);
        }

        var copy = component.Copy();
        copy.Fields = rows;
        return GenericResult<PipelineComponentConfiguration>.Success(copy);
    }

    private static IGenericResult<PipelineComponentConfiguration> Apply(
        PipelineComponentConfiguration component, IReadOnlyList<(string Setting, string Text)> named,
        IReadOnlyDictionary<string, object?> parameters, ILogger logger)
    {
        var settings = named.Where(setting => ParameterSettings.HoldsParameter(setting.Text)).ToList();
        if (settings.Count == 0)
        {
            return GenericResult<PipelineComponentConfiguration>.Success(component);
        }

        var copy = component.Copy();
        foreach (var (setting, text) in settings)
        {
            var rendered = Text(component.Name, setting, text, parameters, logger);
            if (rendered.IsFailure || rendered.Value is null)
            {
                return rendered.ToNewResult<PipelineComponentConfiguration>();
            }

            Set(copy, setting, rendered.Value);
        }

        return GenericResult<PipelineComponentConfiguration>.Success(copy);
    }

    /// <summary>Renders the values into a text: each <c>{{Name}}</c> becomes the value of that name.</summary>
    /// <param name="component">The component, for the failure.</param>
    /// <param name="setting">The setting the text belongs to, for the failure.</param>
    /// <param name="text">The text.</param>
    /// <param name="parameters">The values by name (the run's parameters and the variables no step changes).</param>
    /// <param name="logger">The logger the failure is written to.</param>
    /// <returns>The rendered text, or the failure that names the component, the setting and the name that has no value.</returns>
    internal static IGenericResult<string> Text(
        string component, string setting, string text, IReadOnlyDictionary<string, object?> parameters, ILogger logger)
    {
        string? missing = null;
        var rendered = Reference().Replace(text, match =>
        {
            var name = match.Groups["name"].Value;
            var found = parameters.FirstOrDefault(pair => string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase));
            if (found.Key is null || found.Value is null)
            {
                missing ??= name;
                return string.Empty;
            }

            return Convert.ToString(found.Value, CultureInfo.InvariantCulture) ?? string.Empty;
        });
        return missing is null
            ? GenericResult<string>.Success(rendered)
            : GenericResult<string>.Failure(GraphLog.ParameterNotGiven(logger, component, setting, missing));
    }

    // Why the registry and no switch: the catalogue names the settings that take text, and the registry is where each is kept; a setting the
    // registry does not know is a catalogue error, not a case to guess.
    private static void Set(PipelineComponentConfiguration copy, string setting, string value) =>
        (ComponentSettingBindings.Find(setting) ?? throw new InvalidOperationException($"The catalogue names the setting '{setting}', which no component keeps."))
            .Set(copy, value);
}
