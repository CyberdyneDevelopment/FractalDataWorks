using Fdw.Data.Abstractions;

namespace Fdw.Data.RowSources.Tests;

/// <summary>
/// A container's format resolves through <see cref="FormatTypes"/> while its reader and writer are found by name in
/// <see cref="RecordSourceTypes"/> and <see cref="RecordWriterTypes"/> (FDW-1315). One name per file format, or a container of
/// that format resolves to no format.
/// </summary>
public class FileFormatNamesTests
{
    private static readonly string[] FileFormats = ["Json", "Xml", "Delimited", "FixedWidth", "Xlsx"];

    // A format that is not read as a file has no record source: Tabular is a SQL result set.
    private static readonly string[] NotFileFormats = ["Tabular"];

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("Json")]
    [InlineData("Xml")]
    [InlineData("Delimited")]
    [InlineData("FixedWidth")]
    [InlineData("Xlsx")]
    public void EveryFileFormatHasOneNameInTheFormatsTheReadersAndTheWriters(string name)
    {
        // Act / Assert
        FormatTypes.ByName(name).ShouldNotBeSameAs(FormatTypes.NotFound);
        RecordSourceTypes.ByName(name).ShouldNotBeSameAs(RecordSourceTypes.NotFound);
        RecordWriterTypes.ByName(name).ShouldNotBeSameAs(RecordWriterTypes.NotFound);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void EveryFileFormatIsReadAndWrittenByTheRecordSourceAndWriterOfItsFamily()
    {
        // Act
        var formats = FormatTypes.All()
            .Where(f => !ReferenceEquals(f, FormatTypes.NotFound) && !NotFileFormats.Contains(f.Name, StringComparer.Ordinal))
            .ToList();

        // Assert: Csv, Tsv and Delimited share the family Delimited; every other format is its own family.
        formats.ShouldNotBeEmpty();
        foreach (var format in formats)
        {
            RecordSourceTypes.ByName(format.Family).ShouldNotBeSameAs(RecordSourceTypes.NotFound, format.Name);
            RecordWriterTypes.ByName(format.Family).ShouldNotBeSameAs(RecordWriterTypes.NotFound, format.Name);
        }
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("Csv", "Delimited", ",", ".csv")]
    [InlineData("Tsv", "Delimited", "\t", ".tsv")]
    [InlineData("Delimited", "Delimited", null, ".csv")]
    [InlineData("FixedWidth", "FixedWidth", null, ".txt")]
    public void TheDelimitedFamilyHasNamedFormatsEachWithItsOwnSeparatorAndExtension(
        string name, string family, string? separator, string extension)
    {
        // Act
        var format = FormatTypes.ByName(name);

        // Assert
        format.Family.ShouldBe(family);
        format.Separator.ShouldBe(separator);
        format.CanonicalFileExtension.ShouldBe(extension);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void EveryRegisteredFileRecordSourceIsAFormat()
    {
        // Act
        var sources = RecordSourceTypes.All()
            .Select(s => s.Name)
            .Where(n => FileFormats.Contains(n, StringComparer.Ordinal))
            .ToList();

        // Assert: the sources that read a file are the five formats, no more and no fewer.
        sources.OrderBy(n => n, StringComparer.Ordinal).ShouldBe(FileFormats.OrderBy(n => n, StringComparer.Ordinal));
    }
}
