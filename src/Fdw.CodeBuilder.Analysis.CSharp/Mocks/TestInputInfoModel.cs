using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Fdw.CodeBuilder.Abstractions;
using Microsoft.CodeAnalysis;

namespace Fdw.CodeBuilder.Analysis.CSharp.Mocks;

/// <summary>
/// Mock implementation of <see cref="IInputInfoModel"/>.
/// </summary>
public class TestInputInfoModel : IInputInfoModel
{
    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the value.
    /// </summary>
    public int Value { get; set; }

    /// <summary>
    /// Gets the input hash.
    /// </summary>
    public string InputHash => CalculateInputHash();

    /// <summary>
    /// Calculates the input hash.
    /// </summary>
    /// <returns>The calculated hash.</returns>
    private string CalculateInputHash()
    {
        using var writer = new StringWriter();
        WriteToHash(writer);
        return writer.ToString();
    }

    /// <summary>
    /// Writes the model state to a TextWriter for hash calculation.
    /// </summary>
    /// <param name="writer">The text writer to write to.</param>
    public void WriteToHash(TextWriter writer)
    {
        writer.Write(Name);
        writer.Write(Value);
    }
}