using System.Collections.Generic;
using Fdw.Results;
using Fdw.Services.Descriptions.Abstractions;
using Fdw.Services.Descriptions.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Descriptions;

/// <summary>What every description type must state before the host runs (FDW-6208).</summary>
/// <remarks>
/// A type option is built before the host starts, where a refusal could only be an exception and would end the host without a message, so a type is built as declared
/// and checked here, when the Descriptions service type initializes: the host stops with a coded message naming the type.
/// </remarks>
public static class DescriptionTypeRequirements
{
    /// <summary>Checks that every type states the permission that reads a thing of it.</summary>
    /// <param name="types">The description types.</param>
    /// <param name="logger">The logger the refusal is recorded by.</param>
    /// <returns>Success, or the coded failure naming the first type that states none.</returns>
    public static IGenericResult ReadPermissionsStated(IEnumerable<IDescriptionType> types, ILogger logger)
    {
        foreach (var type in types)
        {
            if (string.IsNullOrWhiteSpace(type.ReadPermission))
            {
                return GenericResult.Failure(DescriptionsLog.ReadPermissionNotStated(logger, type.Name));
            }
        }

        return GenericResult.Success();
    }
}
