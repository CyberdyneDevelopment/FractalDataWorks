using System;
using Fdw.Collections;

namespace Fdw.Web.RestEndpoints.EndpointOptions;

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
}
