using System;

namespace Fdw.Conventions;

/// <summary>
/// Marks a configuration property as a choice only the caller can make (FDW-3823): it has no default anywhere, not in code, not in a setting
/// and not on its service type, and a create request that leaves it out is refused, naming it.
/// </summary>
/// <remarks>
/// Mike, 2026-11-04: "why in the fuck would you make a selection that is a default", and "you are breaking input validation by setting a
/// default". Used where a validator cannot see that a value was left out, such as a flag or a number that binds as false or 0; a text choice is
/// also refused by its validator's NotEmpty rule. The create path (<c>ConfigurationCreateDefaults</c>) reads it on the request body, before binding.
/// </remarks>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class CallerChoiceAttribute : Attribute
{
}
