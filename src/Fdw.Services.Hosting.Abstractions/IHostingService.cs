using Fdw.Services.Abstractions;

namespace Fdw.Services.Hosting.Abstractions;

/// <summary>
/// A host: one configured HTTP request pipeline, resolved by name like any other service.
/// </summary>
public interface IHostingService : IServiceOption
{
}
