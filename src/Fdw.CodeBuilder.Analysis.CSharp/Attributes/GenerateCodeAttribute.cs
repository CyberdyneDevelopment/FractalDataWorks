using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.CodeBuilder.Analysis.CSharp.Attributes;

/// <summary>
/// Attribute used to mark classes for code generation in tests.
/// </summary>
/// <remarks>
/// This code is excluded from code coverage because it is test infrastructure code that supports testing but is not production code.
/// </remarks>
[AttributeUsage(AttributeTargets.Class)]
[ExcludeFromCodeCoverage]
public sealed class GenerateCodeAttribute : Attribute
{
}
