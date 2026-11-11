using System;

namespace Fdw.Conventions;

/// <summary>
/// Marks a configuration property whose default is resolved each time its service is built, never written into the stored row (FDW-4904): a
/// connect, command, request, probe or session-context timeout. A create request that leaves it out stores nothing for it.
/// </summary>
/// <remarks>
/// Mike, 2026-11-10: "the default values should be set in the connection type option (so that it can be over written in the program.cs
/// PlatformServices.... convention) and it should be configurable on the connection". The create path (<c>ConfigurationCreateDefaults</c>)
/// fills every other tuning default from the service type into the body, which freezes the value in force at creation; a property with this
/// attribute is left out instead, so the factory takes the type's default as the host resolved it at registration, and a later registration
/// override reaches the row. A value the configuration states wins.
/// </remarks>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class ResolvedAtUseAttribute : Attribute
{
}
