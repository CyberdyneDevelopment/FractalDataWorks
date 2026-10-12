using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Fdw.Services.Authentication.Validation;

/// <summary>
/// The issuer-to-scheme bindings this host has taken, as they are added.
/// </summary>
/// <remarks>
/// Which issuers a host trusts is configuration, and configuration is read through a gateway, which
/// exists only once the container is built. So the bindings cannot be service registrations made
/// while the container is still being described — they are added during Initialize, and the selector
/// reads them from here on each request.
/// </remarks>
public sealed class AuthenticationSchemeBindings
{
    private readonly ConcurrentDictionary<string, AuthenticationSchemeBinding> _bindings
        = new(System.StringComparer.Ordinal);

    /// <summary>Takes a binding, keyed on the scheme it names.</summary>
    /// <param name="binding">The binding to take.</param>
    /// <returns><see langword="true"/> when it was taken; <see langword="false"/> when that scheme is already bound.</returns>
    public bool Add(AuthenticationSchemeBinding binding)
        => binding is not null && _bindings.TryAdd(binding.SchemeName, binding);

    /// <summary>Gets the bindings taken so far.</summary>
    public IReadOnlyCollection<AuthenticationSchemeBinding> All => (IReadOnlyCollection<AuthenticationSchemeBinding>)_bindings.Values;

    /// <summary>Gets how many bindings have been taken.</summary>
    public int Count => _bindings.Count;
}
