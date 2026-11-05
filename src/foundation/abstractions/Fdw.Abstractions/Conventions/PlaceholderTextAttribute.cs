using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Conventions;

/// <summary>
/// Marks a static class whose <c>const string</c> fields are placeholder text: text shown where a value is absent
/// that can never be a valid value itself, such as "(none)". FDW055 accepts <c>x ?? SomePlaceholders.None</c> when
/// the field's class carries this attribute, and reports a bare string literal on the right of <c>??</c> always.
/// </summary>
/// <remarks>
/// Why a declared class and not a literal: an analyzer cannot tell a placeholder literal from a real value, so the
/// placeholders are named once where they are read and reviewed. A placeholder is never a default message: a missing
/// message is fixed where the message is made (FDW-3580).
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
[ExcludeFromCodeCoverage]
public sealed class PlaceholderTextAttribute : Attribute
{
}
