using Fdw.Data.RowSources;
using Fdw.Data.RowSources.Abstractions;

namespace Fdw.Data.RowSources.Tests;

/// <summary>
/// Content negotiation over the registered record writers: turning an <c>Accept</c> header into a
/// format is the mechanism an endpoint uses to serve any registered format rather than one hardcoded
/// one.
/// </summary>
public class RecordWriterTypesMediaTypeTests
{
    [Theory]
    [Trait("Priority", "P1")]
    [InlineData("application/json", "Json")]
    [InlineData("application/xml", "Xml")]
    [InlineData("text/csv", "Delimited")]
    public void A_registered_media_type_resolves_to_its_format(string mediaType, string expectedFormat)
        => RecordWriterTypes.ByMediaType(mediaType).Format.ShouldBe(expectedFormat);

    [Fact]
    [Trait("Priority", "P1")]
    public void Media_type_matching_ignores_case_and_parameters()
        => RecordWriterTypes.ByMediaType("TEXT/CSV; charset=utf-8").Format.ShouldBe("Delimited");

    [Fact]
    [Trait("Priority", "P1")]
    public void An_unserved_media_type_is_NotFound()
        => RecordWriterTypes.ByMediaType("application/pdf").ShouldBeSameAs(RecordWriterTypes.NotFound);

    [Fact]
    [Trait("Priority", "P1")]
    public void A_format_that_declares_no_media_type_is_never_matched()
    {
        // FixedWidth needs a width per field, which a record schema does not carry, so it declares no
        // media type and must stay unreachable over content negotiation.
        RecordWriterTypes.ByName("FixedWidth").MediaType.ShouldBeEmpty();
        RecordWriterTypes.ByMediaType("").ShouldBeSameAs(RecordWriterTypes.NotFound);
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void An_accept_header_resolves_its_single_media_type()
        => RecordWriterTypes.ByAcceptHeader("text/csv").Format.ShouldBe("Delimited");

    [Fact]
    [Trait("Priority", "P1")]
    public void The_highest_q_value_wins_regardless_of_order()
        => RecordWriterTypes.ByAcceptHeader("application/json;q=0.3, text/csv;q=0.9").Format.ShouldBe("Delimited");

    [Fact]
    [Trait("Priority", "P1")]
    public void An_unserved_first_choice_falls_through_to_a_served_later_one()
        => RecordWriterTypes.ByAcceptHeader("application/pdf, application/json;q=0.5").Format.ShouldBe("Json");

    [Fact]
    [Trait("Priority", "P1")]
    public void A_malformed_q_value_ranks_lowest_rather_than_highest()
    {
        // A typo must not outrank a well-formed preference.
        RecordWriterTypes.ByAcceptHeader("application/json;q=banana, text/csv;q=0.2").Format.ShouldBe("Delimited");
    }

    [Theory]
    [Trait("Priority", "P1")]
    [InlineData("*/*")]
    [InlineData("text/*")]
    [InlineData("")]
    [InlineData(null)]
    public void A_wildcard_states_no_preference_and_resolves_to_NotFound(string? acceptHeader)
    {
        // The endpoint states which format it serves when the client did not choose. Resolving a
        // wildcard here would mean this method picking one on the endpoint's behalf.
        RecordWriterTypes.ByAcceptHeader(acceptHeader).ShouldBeSameAs(RecordWriterTypes.NotFound);
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void Every_registered_format_builds_its_own_options_type_carrying_the_columns()
    {
        string[] columns = ["id", "name"];

        foreach (var format in RecordWriterTypes.All())
        {
            var options = format.CreateOptions(columns);

            options.ShouldNotBeNull();
            options.Columns.ShouldBe(columns, $"{format.Format} dropped the column list");
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void A_writer_built_from_its_own_options_accepts_the_columns_it_requires()
    {
        // Delimited fails loud without a column list, so this proves CreateOptions produces the
        // concrete type CreateWriter casts to - the whole reason it exists.
        var delimited = RecordWriterTypes.ByName("Delimited");
        using var target = new StringWriter();

        using var writer = delimited.CreateWriter(target, delimited.CreateOptions(["id", "name"]));

        writer.ShouldNotBeNull();
    }

    [Theory]
    [Trait("Priority", "P1")]
    [InlineData("Json", "json")]
    [InlineData("Xml", "xml")]
    [InlineData("Delimited", "csv")]
    [InlineData("FixedWidth", "txt")]
    public void EachFormatDeclaresTheExtensionItIsSavedUnder(string format, string expected)
        => RecordWriterTypes.ByName(format).FileExtension.ShouldBe(expected);

    [Fact]
    [Trait("Priority", "P1")]
    public void NoRegisteredFormatLeavesItsExtensionEmptyOrDotted()
    {
        // A download is named "<resource>.<extension>"; an empty or dot-led extension names it wrongly.
        foreach (var format in RecordWriterTypes.All())
        {
            format.FileExtension.ShouldNotBeNullOrWhiteSpace($"{format.Format} declares no extension");
            format.FileExtension.ShouldNotStartWith(".", Case.Sensitive, $"{format.Format} includes the dot");
        }
    }
}
