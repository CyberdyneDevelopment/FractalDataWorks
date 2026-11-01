using Fdw.Services.Abstractions;

namespace Fdw.Services.Hosts.Abstractions;

/// <summary>
/// A host: one configured HTTP request pipeline, resolved by name like any other service.
/// </summary>
public interface IHostService : IServiceOption
{
}
