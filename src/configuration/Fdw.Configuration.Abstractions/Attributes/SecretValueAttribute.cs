using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Configuration;

/// <summary>
/// Says that a setting holds a secret value itself (a password, an API key, a client secret). A client draws it as a password field and a
/// read of the configuration answers it as a mask, never as its value.
/// </summary>
/// <remarks>
/// A setting that holds the NAME of a secret or of the secret manager that keeps it (SecretManagerName, SecretKeyName) is not a secret and
/// does not carry this attribute. Whether a setting is secret is decided by this attribute alone, not by what its name contains.
/// </remarks>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
[ExcludeFromCodeCoverage]
public sealed class SecretValueAttribute : Attribute
{
}
