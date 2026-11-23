using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Graphs.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Graphs.Building;

/// <summary>
/// What the builders of the component types need from the host (FDW-6132): read for a stored component before its graph is built, and for every collected
/// builder when the host starts.
/// </summary>
public static class FlowBuildNeeds
{
    /// <summary>Gives the contribution types the builder of a stored component's component type needs.</summary>
    /// <param name="component">The stored component.</param>
    /// <returns>The types; none for a component type nothing builds, which the build refuses for itself.</returns>
    public static IReadOnlyList<Type> Of(PipelineComponentConfiguration component)
    {
        var kind = ComponentTypes.ByName(component.ComponentType);
        return kind.IsAction ? FlowStepBuilders.ByName(component.ComponentType).Needs : FlowComponentBuilders.ByName(component.ComponentType).Needs;
    }

    /// <summary>Lists, for every builder, each contribution it needs that the host did not register.</summary>
    /// <param name="steps">The builders of the action and loop component types.</param>
    /// <param name="components">The builders of the types that move rows.</param>
    /// <param name="registered">The contributions the host registered.</param>
    /// <param name="logger">The logger each gap is written to.</param>
    /// <returns>One coded message for each component type and contribution not registered; empty when there is none.</returns>
    public static IReadOnlyList<IGenericMessage> Gaps(
        IEnumerable<IFlowStepBuilder> steps, IEnumerable<IFlowComponentBuilder> components, FlowBuildContributions registered, ILogger logger) =>
        [.. steps.Select(step => (step.Name, step.Needs))
            .Concat(components.Select(component => (component.Name, component.Needs)))
            .SelectMany(builder => registered.Unmet(builder.Needs).Select(need => GraphLog.ComponentServicesUnregistered(logger, builder.Name, need.ToString())))];

    /// <summary>
    /// Registers every type a collected builder needs as a build contribution (FDW-6240), once each, so the package of a component type names its contributions in its
    /// builder's <c>Needs</c> and registers nothing.
    /// </summary>
    /// <param name="services">The host's services.</param>
    /// <param name="steps">The builders of the action and loop component types.</param>
    /// <param name="components">The builders of the types that move rows.</param>
    /// <param name="logger">The logger each registration and refusal is written to.</param>
    /// <returns>Success, or one ComponentNeedNotAContribution for each builder and type that is not a concrete contribution; nothing is registered then.</returns>
    /// <remarks>
    /// A contribution is scoped, as the graph pipeline factory that takes it is: what its constructor takes may be a service of the run, read under the run's
    /// person. A dependency the host cannot give is refused at Initialize (<see cref="Resolve"/>).
    /// </remarks>
    public static IGenericResult Register(IServiceCollection services, IEnumerable<IFlowStepBuilder> steps, IEnumerable<IFlowComponentBuilder> components, ILogger logger)
    {
        List<(string Builder, Type Need)> needs = [.. steps.SelectMany(step => step.Needs.Select(need => (step.Name, need)))
            .Concat(components.SelectMany(component => component.Needs.Select(need => (component.Name, need))))];
        List<IGenericMessage> refused = [.. needs.Where(pair => !IsContribution(pair.Need))
            .Select(pair => GraphLog.ComponentNeedNotAContribution(logger, pair.Builder, pair.Need.ToString()))];
        if (refused.Count > 0)
        {
            return GenericResult.Failure(refused);
        }

        foreach (var (builder, need) in needs.DistinctBy(pair => pair.Need))
        {
            services.TryAddEnumerable(ServiceDescriptor.Scoped(typeof(IFlowBuildContribution), need));
            _ = GraphLog.ComponentContributionRegistered(logger, need.ToString(), builder);
        }

        return GenericResult.Success();
    }

    /// <summary>
    /// Constructs the registered build contributions when the host starts (FDW-6240): a contribution registered more than once, and one that cannot be
    /// constructed, are refused, each with its own code.
    /// </summary>
    /// <param name="services">The services of a scope of the host's: contributions are scoped.</param>
    /// <param name="logger">The logger each refusal is written to.</param>
    /// <returns>The contributions, or ComponentContributionUnresolvable, or one ComponentContributionRegisteredTwice for each type registered more than once.</returns>
    public static IGenericResult<FlowBuildContributions> Resolve(IServiceProvider services, ILogger logger)
    {
        List<IFlowBuildContribution> resolved;
        try
        {
            resolved = [.. services.GetServices<IFlowBuildContribution>()];
        }
        catch (InvalidOperationException exception)
        {
            return GenericResult<FlowBuildContributions>.Failure(GraphLog.ComponentContributionUnresolvable(logger, exception, exception.Message));
        }

        List<IGenericMessage> repeated = [.. resolved.GroupBy(contribution => contribution.GetType()).Where(group => group.Count() > 1)
            .Select(group => GraphLog.ComponentContributionRegisteredTwice(logger, group.Key.ToString(), group.Count()))];
        return repeated.Count == 0
            ? GenericResult<FlowBuildContributions>.Success(new FlowBuildContributions(resolved))
            : GenericResult<FlowBuildContributions>.Failure(repeated);
    }

    /// <summary>Lists, as warnings, the registered contributions no collected builder needs (FDW-6240); the host starts all the same.</summary>
    /// <param name="registered">The contributions the host constructed.</param>
    /// <param name="steps">The builders of the action and loop component types.</param>
    /// <param name="components">The builders of the types that move rows.</param>
    /// <param name="logger">The logger each warning is written to.</param>
    /// <returns>One ComponentContributionUnused for each; empty when every contribution is needed.</returns>
    public static IReadOnlyList<IGenericMessage> Unused(
        FlowBuildContributions registered, IEnumerable<IFlowStepBuilder> steps, IEnumerable<IFlowComponentBuilder> components, ILogger logger)
    {
        var needed = new HashSet<Type>(steps.SelectMany(step => step.Needs).Concat(components.SelectMany(component => component.Needs)));
        return [.. registered.Types.Where(type => !needed.Contains(type)).Select(type => GraphLog.ComponentContributionUnused(logger, type.ToString()))];
    }

    // A type the container can construct as a contribution: a class that is not abstract and implements the contract.
    private static bool IsContribution(Type need) =>
        need.IsClass && !need.IsAbstract && typeof(IFlowBuildContribution).IsAssignableFrom(need);
}
