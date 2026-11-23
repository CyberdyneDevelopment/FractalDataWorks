using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Conventions;

/// <summary>
/// Marks a constant that is a limit written in code and has a filed story to make it configuration. FDW073 accepts a <c>Max*</c>, <c>Maximum*</c>,
/// <c>Default*</c> or <c>Limit*</c> constant that carries this attribute with the story's key, so the limits found before the rule existed do not stop
/// the build while their stories are open.
/// </summary>
/// <remarks>
/// Why it names the story: a limit is configuration (Mike, 2026-11-04), and this mark is not an exemption but a debt with its record. The story's last step
/// removes the mark, and then FDW073 holds the constant to the rule. A new limit cannot use it without a filed story. A key that is not a project key and a
/// number (<c>FDW-7103</c>) is itself reported (FDW-7059).
/// </remarks>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
[ExcludeFromCodeCoverage]
public sealed class ConfigurationPendingAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationPendingAttribute"/> class.
    /// </summary>
    /// <param name="item">The key of the story that makes the limit configuration, such as <c>FDW-7103</c>.</param>
    public ConfigurationPendingAttribute(string item)
    {
        Item = item;
    }

    /// <summary>
    /// Gets the key of the story that makes the limit configuration.
    /// </summary>
    public string Item { get; }
}
