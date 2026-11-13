using System.Reflection;
using Microsoft.CodeAnalysis;
using Shouldly;
using Xunit;

namespace Fdw.MessageLogging.Generators.Tests;

/// <summary>
/// A diagnostic that reaches a developer says what is wrong: every descriptor has its title and message text from the resource file
/// (FDW-5581). A resource manager that cannot find the file gives every descriptor an empty text, and nothing else fails.
/// </summary>
public sealed class DiagnosticDescriptorTextTests
{
    private static List<DiagnosticDescriptor> All() =>
        typeof(DiagnosticDescriptors).GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(property => property.PropertyType == typeof(DiagnosticDescriptor))
            .Select(property => (DiagnosticDescriptor)property.GetValue(null)!)
            .ToList();

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void EveryDescriptorHasATitleAndAMessageFormat()
    {
        All().Count.ShouldBeGreaterThanOrEqualTo(27);

        foreach (var descriptor in All())
        {
            descriptor.Title.ToString().ShouldNotBeNullOrWhiteSpace(descriptor.Id + " title");
            descriptor.MessageFormat.ToString().ShouldNotBeNullOrWhiteSpace(descriptor.Id + " message");
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void EveryDescriptorHasAUniqueId()
    {
        var ids = All().Select(descriptor => descriptor.Id).ToList();

        ids.ShouldBe(ids.Distinct(StringComparer.Ordinal).ToList());
    }
}
