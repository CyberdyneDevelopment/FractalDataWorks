using System;

namespace Fdw.Security.Encryption;

/// <summary>A resolved key and when it was fetched, so the cache can tell when it has expired.</summary>
/// <param name="Key">The 256-bit key material.</param>
/// <param name="LoadedAt">When the key was read from its secret manager.</param>
internal sealed record CachedKey(byte[] Key, DateTimeOffset LoadedAt);
