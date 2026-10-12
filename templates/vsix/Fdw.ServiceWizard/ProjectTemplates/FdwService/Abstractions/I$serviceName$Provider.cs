using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Results;

namespace $namespace$.$serviceName$.Abstractions;

/// <summary>
/// Provider interface for resolving $serviceName$ service instances.
/// </summary>
public interface I$serviceName$Provider
{
    Task<IGenericResult<I$serviceName$Service>> GetServiceAsync(string name);
    Task<IGenericResult<I$serviceName$Service>> GetServiceAsync(I$serviceName$Configuration configuration);
    IReadOnlyList<string> GetAvailableServices();
}
