using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Fdw.Hosting.WebMcp;

/// <summary>
/// Internal carrier for assemblies passed to <c>AddWebMcp</c>,
/// held in DI until <c>MapWebMcp</c> triggers discovery.
/// </summary>
// Why: pure data carrier — a single property assigned from the primary constructor, no logic.
[ExcludeFromCodeCoverage]
internal sealed class WebMcpAssemblyList(IReadOnlyList<Assembly> assemblies)
{
    public IReadOnlyList<Assembly> Assemblies { get; } = assemblies;
}
