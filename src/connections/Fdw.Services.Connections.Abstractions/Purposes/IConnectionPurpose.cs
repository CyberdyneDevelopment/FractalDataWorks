using Fdw.Collections;

namespace Fdw.Services.Connections.Abstractions.Purposes;

/// <summary>What a connection is granted and assigned for (FDW-3673), such as Documents or DocumentRecords.</summary>
public interface IConnectionPurpose : ITypeOption<int, ConnectionPurposeBase>
{
    /// <summary>Gets whether a grant or an assignment may narrow the connection to a folder.</summary>
    bool TakesFolder { get; }

    /// <summary>Says why a connection does not fit the purpose, or null when it does.</summary>
    /// <param name="connection">The connection, opened.</param>
    /// <returns>The reason, or null.</returns>
    string? Unfit(IGenericConnection connection);
}
