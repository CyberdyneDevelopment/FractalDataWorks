using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.DataSets.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.DataSets.Tests;

/// <summary>
/// FDW-5287: a Graph data set stores its fields out in its own table, and still hands every reader of the shared data set
/// configuration the shared field type.
/// </summary>
public class GraphDataSetConfigurationTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void TheGeneratedMapperOfAGraphComponentCascadesItsFieldMappingsColumnListsSplitOutputsAndStage()
    {
        var children = new GraphDataSetComponentConfigurationPocoMapper().CascadeChildren.Select(child => child.ChildType).ToList();

        children.Count.ShouldBe(4);
        children.ShouldContain(typeof(GraphDataSetComponentFieldConfiguration));
        children.ShouldContain(typeof(GraphDataSetComponentColumnConfiguration));
        children.ShouldContain(typeof(GraphDataSetComponentSplitOutputConfiguration));
        children.ShouldContain(typeof(GraphDataSetStageConfiguration));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void TheGeneratedMapperOfAFieldMappingCascadesItsTransformationChain()
    {
        new GraphDataSetComponentFieldConfigurationPocoMapper().CascadeChildren.Select(child => child.ChildType)
            .ShouldBe([typeof(GraphDataSetComponentFieldTransformationConfiguration)]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void TheGeneratedMapperOfAStageCascadesEveryListOfTheStage()
    {
        new GraphDataSetStageConfigurationPocoMapper().CascadeChildren.Count.ShouldBe(10);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void SettingTheFieldsKeepsWhatFeedsAndTheNotesOfAFieldOfTheSameName()
    {
        var graph = new GraphDataSetConfiguration();
        graph.GraphFields.Add(new GraphDataSetFieldConfiguration
        {
            Name = "Amount",
            FedByComponent = "Join1",
            FedByField = "Total",
            Notes = [new GraphDataSetFieldNoteConfiguration { NoteKind = "Question", Body = "Which tariff?" }],
        });

        graph.Fields = [new DataSetFieldConfiguration { Name = "Amount", TypeName = "Decimal" }, new DataSetFieldConfiguration { Name = "Other" }];

        var kept = graph.GraphFields.Single(f => f.Name == "Amount");
        (kept.FedByComponent, kept.FedByField, kept.TypeName).ShouldBe(("Join1", "Total", "Decimal"));
        kept.Notes.Single().Body.ShouldBe("Which tariff?");
        graph.GraphFields.Single(f => f.Name == "Other").Notes.ShouldBeEmpty();
    }

    /// <summary>What a reader of any kind of data set sees: the fields through <see cref="IDataSetImplementationConfiguration"/>, not through the Graph type.</summary>
    /// <param name="dataSet">The data set, taken as the shared interface every reader takes.</param>
    /// <returns>Its fields.</returns>
    public static IList<DataSetFieldConfiguration> FieldsAsAReaderSees(IDataSetImplementationConfiguration dataSet) => dataSet.Fields;

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void TheGeneratedMapperOfAGraphDataSetCascadesItsFieldsOutItsComponentsItsLinksAndItsIntents()
    {
        var children = new GraphDataSetConfigurationPocoMapper().CascadeChildren.Select(child => child.ChildType).ToList();

        children.Count.ShouldBe(4);
        children.ShouldContain(typeof(GraphDataSetFieldConfiguration));
        children.ShouldContain(typeof(GraphDataSetComponentConfiguration));
        children.ShouldContain(typeof(GraphDataSetLinkConfiguration));
        children.ShouldContain(typeof(GraphDataSetIntentConfiguration));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void TheFieldsAReaderSeesAreACopyOfTheStoredFieldsOut()
    {
        var graph = new GraphDataSetConfiguration();
        graph.GraphFields.Add(new GraphDataSetFieldConfiguration
        {
            Id = Guid.NewGuid(),
            Name = "Amount",
            TypeName = "Decimal",
            Ordinal = 1,
            FedByComponent = "Join1",
            FedByField = "Amount"
        });

        var fields = FieldsAsAReaderSees(graph);

        fields.Count.ShouldBe(1);
        fields[0].Name.ShouldBe("Amount");
        fields[0].TypeName.ShouldBe("Decimal");
        fields[0].Ordinal.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void SettingTheFieldsReplacesTheStoredFieldsOut()
    {
        var graph = new GraphDataSetConfiguration();
        graph.GraphFields.Add(new GraphDataSetFieldConfiguration { Id = Guid.NewGuid(), Name = "Old" });

        graph.Fields = [new DataSetFieldConfiguration { Id = Guid.NewGuid(), Name = "Revenue", TypeName = "Decimal" }];

        graph.GraphFields.Count.ShouldBe(1);
        graph.GraphFields[0].Name.ShouldBe("Revenue");
        graph.GraphFields[0].TypeName.ShouldBe("Decimal");
        graph.GraphFields[0].FedByComponent.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void AGraphDataSetCarriesTheImplementationNameItsDomainRowSays()
    {
        GraphDataSetConfiguration.ImplementationName.ShouldBe("Graph");
    }
}
