namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>
/// A service of a component type's own package that its builder needs (FDW-6132, FDW-6240): one FDW has no domain for, such as a model client or a tool
/// client, taken by the contribution's own constructor.
/// </summary>
/// <remarks>
/// <para>
/// Today a step or component is built by its builder from the component, its built children and the run (<c>FlowStepBuilderBase.Build</c>,
/// <c>FlowBuildContext</c>). Added: this contract, which the package of the component type implements and names in its builder's <c>Needs</c>; the builder reads it with
/// <c>context.Contribution&lt;T&gt;(component)</c>. Neighbor: the providers on <c>FlowBuildContext</c> (<c>ConnectionProvider</c>), which carry what FDW owns;
/// FDW's own contracts are domains, never contributions.
/// </para>
/// <para>
/// No existing pattern fits: a step is not an <c>IGenericService</c>, which a service type requires; <c>IServiceFactory.Create</c> takes only the
/// configuration, while a step is built from the component, its built children and the run; and a service type collection holds only service type
/// registrations, while <c>ComponentTypes</c> stays free of the host.
/// </para>
/// <para>
/// The package of the component type registers nothing. The graph type's Register phase registers every type a collected builder needs as a scoped
/// <c>IFlowBuildContribution</c>, as the graph pipeline factory is, so a contribution may take a service of the run; its Initialize constructs them in a scope:
/// a contribution registered twice, or one that cannot be constructed, stops the host, each with its own code. What a contribution's constructor takes is
/// registered by that service's own domain. A hand registration is refused: FDW056 at compile time in an application, and a second registration at
/// Initialize inside a package. A Graph data set gets them too: its runner is held by the singleton data gateway factories, so each read opens a scope of its
/// own and resolves the contributions in it as the host's start does (two reads never share an instance); one that cannot be constructed fails the read, naming the data set.
/// </para>
/// <example>
/// <code>
/// public sealed class ModelContribution(IModelClient client) : IFlowBuildContribution
/// {
///     public IModelClient Client { get; } = client;
/// }
///
/// // In the builder: public override IReadOnlyList&lt;Type&gt; Needs =&gt; [typeof(ModelContribution)];
/// //                 var model = context.Contribution&lt;ModelContribution&gt;(component);
/// </code>
/// </example>
/// </remarks>
public interface IFlowBuildContribution
{
}
