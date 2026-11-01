using System.Collections.Generic;

namespace Fdw.Services.Data;

/// <summary>
/// A derived value together with the tags its build read.
/// </summary>
/// <remarks>
/// The tags travel with the value so that whoever receives it, from the cache or from a build it joined,
/// can record them in a build of its own. That is what keeps a build inside a build invalidated by
/// the reads of both.
/// </remarks>
/// <typeparam name="T">The kind of value.</typeparam>
/// <param name="Value">The value.</param>
/// <param name="Tags">The invalidation tags of every read that built it.</param>
internal sealed record DerivedEntry<T>(T Value, IReadOnlyList<string> Tags);
