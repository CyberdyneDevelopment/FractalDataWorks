using System;
using Fdw.Collections;
using Fdw.Results;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Fdw.UI.ComponentOptions;

/// <summary>
/// A declared headless component: its provider type, and whether a host wants it registered.
/// </summary>
/// <remarks>
/// Derives from <see cref="ITypeOption{TKey, T}"/> rather than standing alone, and that is
/// load-bearing. TypeCollectionGenerator asks the member interface whether it carries Name and Id
/// and emits stub <c>ByName</c>/<c>ById</c> bodies — literally <c>return NotFound;</c> — when it
/// does not. A stubbed lookup would let <c>SomeComponents.ByName("X").SkipRegistration = true</c>
/// compile and address nothing, which is the worst outcome for a switch whose only job is to be
/// obeyed.
///
/// What "registering" means for a component is worth stating, because it differs from an endpoint.
/// An endpoint goes into DI and gets routed. A headless component is a Blazor provider a skin
/// renders: what it needs registered is whatever its context resolves — a typed client, a cache, an
/// accessor — plus its own assembly, so Blazor's component discovery can find it. Declaring the
/// component is what lets those two happen together instead of in a host's Program.cs, where
/// nothing connects the registration to the component that needs it.
/// </remarks>
public interface IComponentTypeOption : ITypeOption<int, ComponentTypeOptionBase>
{
    /// <summary>
    /// Gets the provider component this option declares.
    /// </summary>
    /// <remarks>
    /// A name cannot be handed to Blazor; the type can. Its assembly is also what
    /// <c>AddAdditionalAssemblies</c> needs, so the type answers both questions at once.
    /// </remarks>
    Type ComponentType { get; }

    /// <summary>
    /// Gets or sets a value indicating whether registration should pass this component over.
    /// </summary>
    /// <remarks>
    /// Skip rather than register, so the default — <c>default(bool)</c> — registers. A
    /// <c>ShouldRegister</c> spelling would leave every option needing explicit initialisation
    /// before behaving normally.
    /// </remarks>
    bool SkipRegistration { get; set; }

    /// <summary>Runs this component's Configure body.</summary>
    /// <param name="builder">The host builder.</param>
    /// <returns>The builder, or a failure the caller decides what to do with.</returns>
    IGenericResult<IHostApplicationBuilder> Configure(IHostApplicationBuilder builder);

    /// <summary>Runs this component's Register body.</summary>
    /// <param name="builder">The host builder.</param>
    /// <param name="loggerFactory">The logger factory, if the host has one yet.</param>
    /// <returns>The builder, or a failure the caller decides what to do with.</returns>
    IGenericResult<IHostApplicationBuilder> Register(IHostApplicationBuilder builder, ILoggerFactory? loggerFactory = null);

    /// <summary>Runs this component's Initialize body.</summary>
    /// <param name="host">The built host.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <returns>The host, or a failure the caller decides what to do with.</returns>
    IGenericResult<IHost> Initialize(IHost host, ILoggerFactory? loggerFactory = null);
}
