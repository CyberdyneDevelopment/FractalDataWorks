using System;
using System.Threading;

namespace Fdw.Services.Data.Abstractions;

/// <summary>
/// The name a host declared for one connection role, held once the host's <c>configurationSchema.json</c> has been
/// read. Until then there is no name: reading one is a defect in the caller, not a case to cover with a default.
/// </summary>
/// <remarks>
/// <see cref="PlatformConnection"/>, <see cref="ServerConnection"/> and <see cref="AuthConnection"/> each hold one.
/// The only writer is the configuration gateway domain's registration, which reads the host's declaration, checks it,
/// and calls <see cref="Declare"/>; a host never sets a name from code.
/// </remarks>
public sealed class DeclaredConnectionName
{
    private readonly string _role;
    private string? _name;

    /// <summary>Initializes a new instance of the <see cref="DeclaredConnectionName"/> class.</summary>
    /// <param name="role">The role this name plays (Platform, Server, Auth), used to say which declaration is missing.</param>
    public DeclaredConnectionName(string role)
    {
        _role = role;
    }

    /// <summary>Gets the role this name plays.</summary>
    public string Role => _role;

    /// <summary>Gets a value indicating whether the host has declared a name for this role.</summary>
    public bool IsDeclared => Volatile.Read(ref _name) is not null;

    /// <summary>Gets the declared name.</summary>
    /// <exception cref="InvalidOperationException">The host has not declared this role yet. Start-up refuses a host that
    /// declares none, so this is reached only by code that runs before the schema is read.</exception>
    public string Name => Volatile.Read(ref _name)
        ?? throw new InvalidOperationException(
            $"The {_role} connection name was read before the host declared it. Declare ConnectionRoles.{_role} in the " +
            "host's configurationSchema.json; it is read when the configuration gateway domain registers.");

    /// <summary>Records the name the host declared.</summary>
    /// <param name="name">The declared connection name.</param>
    /// <returns><see langword="true"/> when the name is now recorded; <see langword="false"/> when a different name was already recorded, because one process has one declaration.</returns>
    public bool Declare(string name)
    {
        var recorded = Interlocked.CompareExchange(ref _name, name, null);
        return recorded is null || string.Equals(recorded, name, StringComparison.Ordinal);
    }
}
