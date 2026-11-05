using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Services.Connections.Abstractions.Purposes;

/// <summary>
/// The base of the purposes a connection is granted to a tenant and assigned to an organization for (FDW-1581, FDW-3673). A purpose says
/// which connections fit it and whether a grant or an assignment may narrow it to a folder.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class ConnectionPurposeBase : TypeOptionBase<ConnectionPurposeBase>, IConnectionPurpose
{
    /// <summary>Initializes the sentinel the collection answers for an unknown name.</summary>
    protected ConnectionPurposeBase()
        : base(0, "NotFound")
    {
    }

    /// <summary>Initializes a new instance of the <see cref="ConnectionPurposeBase"/> class.</summary>
    /// <param name="name">The purpose's name, as grants and assignments store it.</param>
    /// <param name="description">What a connection granted for it is used for.</param>
    /// <param name="takesFolder">Whether a grant or an assignment may narrow the connection to a folder.</param>
    protected ConnectionPurposeBase(string name, string description, bool takesFolder = false)
        : base(name, $"ConnectionPurposes:{name}", name, description, "ConnectionPurpose")
    {
        TakesFolder = takesFolder;
    }

    /// <summary>Gets whether a grant or an assignment may narrow the connection to a folder.</summary>
    public bool TakesFolder { get; }

    /// <summary>Says why a connection does not fit the purpose, or null when it does.</summary>
    /// <remarks>Refuses here, so a purpose that does not say which connections fit it fits none; the collection's sentinel is one.</remarks>
    /// <param name="connection">The connection, opened.</param>
    /// <returns>The reason, or null.</returns>
    public virtual string? Unfit(IGenericConnection connection) => $"the purpose '{Name}' is not one connections are granted for";
}
