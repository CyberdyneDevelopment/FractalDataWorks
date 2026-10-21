using Fdw.Collections;

namespace Fdw.Services.RateLimiting.Abstractions;

/// <summary>
/// Interface defining the contract for rate limit policy options: a policy is a tier of callers, known by its name.
/// </summary>
/// <remarks>
/// What a tier is held to is not here. Its numbers and the names of its algorithm and of the way its callers are counted are
/// settings, read for each request as a <see cref="RateLimitAllowance"/> (FDW-1945); <see cref="RateLimitAllowanceFields"/>
/// names them. Before FDW-1945 each policy carried them as constants, so changing one was a release of every host.
/// </remarks>
public interface IRateLimitPolicy : ITypeOption<int, IRateLimitPolicy>
{
}
