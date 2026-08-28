using System;

namespace Fdw.Collections;

/// <summary>
/// Derives a type option's identity from its name.
/// </summary>
/// <remarks>
/// Shared rather than duplicated: a service type derives its Guid this way, and a collection that is
/// itself a member of a parent collection needs the same answer for the same name. Two
/// implementations of "the id for this name" is two chances for them to disagree, and the disagreement
/// would surface as a configuration row written under one id and looked up under another.
///
/// Derived rather than generated: the same option must be the same identity in every process that
/// loads it. <c>Guid.NewGuid()</c> gives a different answer on each start, so a row written by one run
/// would not be found by the next.
/// </remarks>
public static class OptionId
{
#pragma warning disable CA5351, SCS0006, CA1850
    /// <summary>Derives the identity for <paramref name="name"/>.</summary>
    /// <param name="name">The option's name, or a type's full name for a collection.</param>
    /// <returns>The same Guid for the same name, in every process.</returns>
    public static Guid Derive(string name)
    {
        if (name is null)
        {
            throw new ArgumentNullException(nameof(name));
        }

        using var md5 = System.Security.Cryptography.MD5.Create();
        return new Guid(md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(name)));
    }
#pragma warning restore CA5351, SCS0006, CA1850
}
