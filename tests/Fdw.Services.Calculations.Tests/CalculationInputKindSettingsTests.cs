using System;
using System.Linq;
using System.Reflection;
using Fdw.Calculations.Endpoints.CalculationEntities;
using Fdw.Services.Calculations.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Calculations.Tests;

/// <summary>
/// FDW-3228: each calculation input kind declares the members of an input it takes, so a client draws an input of a kind from them and
/// create and update refuse a member the kind does not declare.
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "CoreFramework")]
public class CalculationInputKindSettingsTests
{
    [Theory]
    [InlineData("DataSet")]
    [InlineData("Scalar")]
    [InlineData("Container")]
    public void EverySettingNamesAMemberOfTheInputDeclaration(string kind)
    {
        var settings = CalculationInputKinds.ByName(kind).Settings;

        settings.ShouldNotBeEmpty();
        foreach (var setting in settings)
        {
            typeof(CalculationEntityInputDto)
                .GetProperty(setting.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
                .ShouldNotBeNull(setting.Name);
        }
    }

    [Theory]
    [InlineData("DataSet")]
    [InlineData("Scalar")]
    [InlineData("Container")]
    public void SettingNamesAreUniqueIgnoringCase(string kind)
    {
        var names = CalculationInputKinds.ByName(kind).Settings.Select(setting => setting.Name).ToList();

        names.Distinct(StringComparer.OrdinalIgnoreCase).Count().ShouldBe(names.Count);
    }

    [Fact]
    public void ADataSetInputTakesTheDataSetItReads()
    {
        var setting = CalculationInputKinds.ByName("DataSet").Settings.ShouldHaveSingleItem();

        setting.Name.ShouldBe("dataSetName");
        setting.Kind.ShouldBe("DataSet");
        setting.IsRequired.ShouldBeTrue();
    }

    [Fact]
    public void AScalarInputTakesATypeChosenFromTheScalarValueTypesAndTheValueAsText()
    {
        var settings = CalculationInputKinds.ByName("Scalar").Settings.ToDictionary(setting => setting.Name);

        settings["scalarValueType"].Kind.ShouldBe("ScalarValueTypes");
        settings["scalarValueType"].IsRequired.ShouldBeTrue();
        settings["scalarSerializedValue"].Kind.ShouldBe("Text");
        settings["scalarSerializedValue"].IsRequired.ShouldBeTrue();
        settings.Count.ShouldBe(2);
    }

    [Fact]
    public void AContainerInputTakesAConnectionAndAPathWithinIt()
    {
        var settings = CalculationInputKinds.ByName("Container").Settings.ToDictionary(setting => setting.Name);

        settings["connectionName"].Kind.ShouldBe("Connection");
        settings["connectionName"].IsRequired.ShouldBeTrue();
        settings["containerPath"].Kind.ShouldBe("Text");
        settings["containerPath"].IsRequired.ShouldBeTrue();
        settings.Count.ShouldBe(2);
    }
}
