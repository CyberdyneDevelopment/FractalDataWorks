using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.MessageLogging;

/// <summary>
/// Declares the default <c>TypeCode</c> (the Code-string prefix, e.g. <c>"MSSQL"</c>) for every
/// <see cref="MessageLoggingAttribute"/> method in the annotated class. A per-method
/// <c>TypeCode</c> still overrides this; when neither is present the generator falls back to
/// <c>"FDW"</c>. Set once per Log class so all its methods emit <c>"{TypeCode}-{EventId}"</c>
/// codes that align with the owning package's result-code prefix.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
[ExcludeFromCodeCoverage]
public sealed class MessageLoggingTypeCodeAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MessageLoggingTypeCodeAttribute"/> class.
    /// </summary>
    /// <param name="typeCode">The default Code-string prefix for the class's logging methods.</param>
    public MessageLoggingTypeCodeAttribute(string typeCode)
    {
        TypeCode = typeCode;
    }

    /// <summary>
    /// Gets the default Code-string prefix applied to the class's logging methods.
    /// </summary>
    public string TypeCode { get; }
}
