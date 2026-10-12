using System;
using Fdw.Collections;
using Fdw.Results;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Fdw.Web.RestEndpoints.EndpointTypeOptions;

/// <summary>
/// A declared endpoint: the endpoint's type, and whether a host wants it registered.
/// </summary>
/// <remarks>
/// This derives from <see cref="ITypeOption{TKey, T}"/> rather than standing alone, and that is
/// load-bearing rather than tidiness. TypeCollectionGenerator asks the member interface whether it
/// carries Name and Id, and emits stub <c>ByName</c>/<c>ById</c> bodies — literally
/// <c>return NotFound;</c> — when it does not. Five command collections in this repo are in exactly
/// that state. A stubbed lookup would make
/// <c>SomeEndpoints.ByName("ListServerSettings").SkipRegistration = true</c> compile and silently
/// address nothing, which is the worst available outcome for a switch whose entire job is to be
/// obeyed.
/// </remarks>
public interface IEndpointTypeOption : ITypeOption<int, EndpointTypeOptionBase>
{
    /// <summary>
    /// Gets the endpoint class this option declares.
    /// </summary>
    /// <remarks>
    /// A name cannot be handed to the container; the type can. Registration resolves this rather
    /// than looking a type up from the option's name by convention, because a convention lookup
    /// fails at startup on a rename the compiler would otherwise have caught.
    /// </remarks>
    Type EndpointType { get; }

    /// <summary>
    /// Gets or sets a value indicating whether registration should pass this endpoint over.
    /// </summary>
    /// <remarks>
    /// Skip rather than register, so the default — <c>default(bool)</c> — registers. A
    /// <c>ShouldRegister</c> spelling would leave every option needing explicit initialisation
    /// before it behaved normally.
    ///
    /// Mutable by design, and consistent with the rest of the model: ServiceTypeBase already carries
    /// mutable phase state (ConfigurationMethod, RegistrationIsCustom) which the gerund setters
    /// replace after construction. The ordering question that raises is answered by the phases
    /// themselves — set this during Configure, which runs before Register reads it.
    /// </remarks>
    bool SkipRegistration { get; set; }

    // The three phases belong on the interface, not just the base, because a collection cycles its
    // members as IEndpointTypeOption — it is the declared contract that has to carry them, or the
    // collect can see an endpoint and not be able to run it.

    /// <summary>Runs this endpoint's Configure body.</summary>
    /// <param name="builder">The host builder.</param>
    /// <returns>The builder, or a failure the caller decides what to do with.</returns>
    /// <param name="force">Run regardless of the skip flag and whether the phase has already run.</param>
    IGenericResult<IHostApplicationBuilder> Configure(IHostApplicationBuilder builder, bool force = false);

    /// <summary>Runs this endpoint's Register body.</summary>
    /// <param name="builder">The host builder.</param>
    /// <param name="loggerFactory">The logger factory, if the host has one yet.</param>
    /// <returns>The builder, or a failure the caller decides what to do with.</returns>
    /// <param name="force">Run regardless of the skip flag and whether the phase has already run.</param>
    IGenericResult<IHostApplicationBuilder> Register(IHostApplicationBuilder builder, ILoggerFactory? loggerFactory = null, bool force = false);

    /// <summary>Runs this endpoint's Initialize body.</summary>
    /// <param name="host">The built host.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <returns>The host, or a failure the caller decides what to do with.</returns>
    /// <param name="force">Run regardless of the skip flag and whether the phase has already run.</param>
    IGenericResult<IHost> Initialize(IHost host, ILoggerFactory? loggerFactory = null, bool force = false);
}
