using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Data;

/// <summary>
/// Keeps a value that is built from configuration reads, so it is not rebuilt on every request.
/// </summary>
/// <remarks>
/// The value is kept under the caller's cache partition of the configuration connection, exactly as
/// every read the gateway caches is, and is dropped when a container the build read is written. Which
/// containers those are is not declared by the caller: the gateway records the reads the build made.
/// Offered by the concrete gateway and deliberately not a member of <c>IConfigurationGateway</c>,
/// which many test doubles implement.
/// </remarks>
public interface IDerivedConfigurationCache
{
    /// <summary>
    /// Returns the value kept under <paramref name="name"/> for the caller, building it with
    /// <paramref name="build"/> when there is none.
    /// </summary>
    /// <typeparam name="T">The kind of value; part of the key, so one name can hold values of different kinds.</typeparam>
    /// <param name="name">What the value is, for example the data store it describes.</param>
    /// <param name="build">Builds the value from configuration reads made through this gateway.</param>
    /// <param name="isWorthKeeping">
    /// Says whether a built value is one to remember. A value built while the caller could see nothing
    /// is a success that describes nothing, and keeping it would make that emptiness last.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the wait; it never cancels a build other callers share.</param>
    /// <returns>The value, freshly built or kept. A failure is returned and never kept.</returns>
    /// <remarks>
    /// A value is not kept when the build read nothing through this gateway, because there would be no
    /// write that could drop it. Callers that share one caller partition share one value.
    /// </remarks>
    Task<IGenericResult<T>> Derive<T>(
        string name,
        Func<Task<IGenericResult<T>>> build,
        Func<T, bool> isWorthKeeping,
        CancellationToken cancellationToken = default);
}
