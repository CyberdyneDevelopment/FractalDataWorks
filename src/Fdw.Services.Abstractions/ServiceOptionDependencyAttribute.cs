using System;

namespace Fdw.Services.Abstractions;

/// <summary>
/// Opts a constructor parameter out of FDW044 (service-option-injects-service-option-directly).
/// Use ONLY when the service-option dependency is supplied already-resolved by the owning
/// provider/factory — e.g. an immutable service constructed by its provider, like a data vault
/// whose connection <c>DataVaultProvider</c> resolves by name in system context. It is NOT
/// a shortcut around injecting <c>IPlatformServiceProvider&lt;TService, TConfiguration&gt;</c> — the
/// default (no attribute) means the parameter must be a provider.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
public sealed class ServiceOptionDependencyAttribute : Attribute
{
}
