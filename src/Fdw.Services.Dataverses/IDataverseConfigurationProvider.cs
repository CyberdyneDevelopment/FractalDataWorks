using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Dataverses;

/// <summary>
/// Reads and writes dataverse configurations.
/// </summary>
/// <remarks>
/// Three Get overloads and nothing else: by name, by id, and all of them. A caller that wants
/// one dataverse's members reads the dataverse and walks it, rather than asking a provider for a
/// filtered slice — the aggregate the provider returns is already navigable.
/// </remarks>
public interface IDataverseConfigurationProvider
{

}
