using Fdw;
using Fdw.Commands.Data.Abstractions;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Abstractions.Commands.ConnectionManagementOperationOptions;

namespace Fdw.Services.Connections.Abstractions.Commands;

/// <summary>
/// Command interface for managing connections (list, remove, etc.).
/// </summary>
public interface IConnectionManagementCommand : IConnectionCommand
{
    /// <summary>
    /// Gets the management operation to perform.
    /// </summary>
    IConnectionManagementOperation Operation { get; }

    /// <summary>
    /// Gets the connection name (optional, depending on operation).
    /// </summary>
    string? ConnectionName { get; }
}
