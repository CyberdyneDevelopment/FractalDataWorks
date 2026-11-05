using System.Collections.Generic;

namespace Fdw.ServiceTypes;

/// <summary>
/// A service type option that declares the tuning defaults of its configuration (FDW-3823): the value a configuration of this type takes
/// for a field it does not state, keyed by the configuration property's name.
/// </summary>
/// <remarks>
/// Mike, 2026-11-04: "most of these should be a default value set in the service type option or collection so that in the registration we
/// can change it", and "the thing you select carries it". Only tuning goes here (a port a type implies, a timeout, a retry count, a pool
/// size, a flag such as encrypt): a value the caller chooses (where, which kind, which operator) has no default and its validator refuses it.
/// A domain's option base declares the values every type of the domain shares (the collection's default); a type adds or replaces its own.
/// A host changes one at registration with <see cref="ServiceTypeDefaults.Override"/>, and a configuration that states a value wins over both.
/// </remarks>
public interface IDeclaresDefaults
{
    /// <summary>Gets the tuning defaults this type declares, by configuration property name.</summary>
    IReadOnlyDictionary<string, object> DeclaredDefaults { get; }
}
