using Fdw.Services.Etl.Flow;

namespace Fdw.Services.Etl.Tests.Graphs;

/// <summary>
/// FDW-5457: before a script is parsed, each <c>$(Name)</c> token is replaced by an identifier that stands for the name the run gives, so the parser
/// sees a name where the run puts one, and the reader knows which names are not known yet.
/// </summary>
public sealed class ScriptTokenMarkTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void EachTokenIsReplacedByItsOwnIdentifier()
    {
        var marks = ScriptTokens.Mark("DELETE FROM $(Schema).$( Table ) WHERE Kind IN ($(Kinds))");

        marks.Markers.Count.ShouldBe(3);
        marks.Markers.ShouldBeUnique();
        marks.Text.ShouldBe($"DELETE FROM {marks.Markers[0]}.{marks.Markers[1]} WHERE Kind IN ({marks.Markers[2]})");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void AnIdentifierIsLettersDigitsAndUnderscoresOnly()
    {
        ScriptTokens.Mark("SELECT 1 FROM $(T)").Markers[0].ShouldMatch("^[A-Za-z0-9_]+$");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void AScriptWithNoTokenIsUnchanged()
    {
        var marks = ScriptTokens.Mark("SELECT Id FROM dbo.Team WHERE Year = @Year");

        marks.Text.ShouldBe("SELECT Id FROM dbo.Team WHERE Year = @Year");
        marks.Markers.ShouldBeEmpty();
    }
}
