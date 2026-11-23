using System.Linq;
using Fdw.Services.Descriptions.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Descriptions.Tests;

/// <summary>The description types and sub-types this package ships, and what each states (FDW-6120).</summary>
[Trait("Category", "Descriptions")]
public sealed class DescriptionTypesTests
{
    [Fact]
    [Trait("Priority", "P1")]
    // Each domain of FDW-6208 that moves its description into the descriptions table adds its own type here, in the commit that adds the type (FDW-6271).
    public void TheShippedTypesAreTheOnesTheMovedDomainsDeclare()
    {
        DescriptionTypes.All().Select(type => type.Name).Order(System.StringComparer.Ordinal).ToArray().ShouldBe(["AuthenticationFlow", "Calculation", "CalculationVersion", "CatalogCategory", "Connection", "DataConfigurationRule", "DataConfigurationRuleValue", "DataContainer", "DataContainerField", "DataContainerKey", "DataPath", "DataSet", "DataSetAggregate", "DataSetAnnotation", "DataSetAnnotationField", "DataSetField", "DataSetJoin", "DataSetSample", "DataStore", "EscalationPolicy", "Identity", "Messaging", "Notification", "NotificationList", "NotificationRule", "OrchestrationNode", "OrganizationRole", "Permission", "Pipeline", "PipelineParameter", "QualityRule", "QualityRuleReferenceValue", "Role", "Schedule", "SecretManager", "ServerSetting", "SqlScript", "Tag", "Theme", "Threshold", "TokenManager", "Transformation", "WebhookTrigger", "Workspace"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void EveryTypeStatesThePermissionThatReadsAThingOfIt()
    {
        foreach (var type in DescriptionTypes.All())
        {
            type.ReadPermission.ShouldNotBeNullOrWhiteSpace($"type '{type.Name}' states no read permission");
        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void OnlyAFieldOrAStoresChildBelongsToAnOwnerThatNamesIt()
    {
        DescriptionTypes.ByName("DataSet").HasOwner.ShouldBeFalse();
        DescriptionTypes.ByName("DataSetField").HasOwner.ShouldBeTrue();
        DescriptionTypes.ByName("DataSetAnnotation").HasOwner.ShouldBeFalse();
        DescriptionTypes.ByName("DataSetAnnotationField").HasOwner.ShouldBeTrue();
        DescriptionTypes.ByName("QualityRule").HasOwner.ShouldBeFalse();
        DescriptionTypes.ByName("QualityRuleReferenceValue").HasOwner.ShouldBeTrue();
        DescriptionTypes.ByName("DataConfigurationRule").HasOwner.ShouldBeFalse();
        DescriptionTypes.ByName("DataConfigurationRuleValue").HasOwner.ShouldBeTrue();
        DescriptionTypes.ByName("DataSetAggregate").HasOwner.ShouldBeTrue();
        DescriptionTypes.ByName("DataSetJoin").HasOwner.ShouldBeTrue();
        DescriptionTypes.ByName("DataSetSample").HasOwner.ShouldBeTrue();
        DescriptionTypes.ByName("Tag").HasOwner.ShouldBeFalse();
        DescriptionTypes.ByName("Transformation").HasOwner.ShouldBeFalse();
        DescriptionTypes.ByName("Workspace").HasOwner.ShouldBeFalse();
        DescriptionTypes.ByName("Calculation").HasOwner.ShouldBeFalse();
        DescriptionTypes.ByName("CalculationVersion").HasOwner.ShouldBeFalse();
        DescriptionTypes.ByName("DataStore").HasOwner.ShouldBeFalse();
        DescriptionTypes.ByName("DataPath").HasOwner.ShouldBeTrue();
        DescriptionTypes.ByName("DataContainer").HasOwner.ShouldBeTrue();
        DescriptionTypes.ByName("DataContainerField").HasOwner.ShouldBeTrue();
        DescriptionTypes.ByName("DataContainerKey").HasOwner.ShouldBeTrue();
        DescriptionTypes.ByName("CatalogCategory").HasOwner.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void ATransformationsDescriptionIsReadByWhoeverReadsDataSets()
    {
        DescriptionTypes.ByName("Transformation").ReadPermission.ShouldBe("datasets:read");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void ATagsDescriptionIsReadByWhoeverReadsTags()
    {
        DescriptionTypes.ByName("Tag").ReadPermission.ShouldBe("tags:read");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AConnectionDescriptionIsReadByWhoeverReadsConnectionsWhateverTheConnectionType()
    {
        DescriptionTypes.ByName("Connection").ReadPermission.ShouldBe("connections:read");
        DescriptionTypes.ByName("Connection").HasOwner.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AScheduleDescriptionIsReadByWhoeverReadsSchedules()
    {
        DescriptionTypes.ByName("Schedule").ReadPermission.ShouldBe("schedules:read");
        DescriptionTypes.ByName("Schedule").HasOwner.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AWorkspacesDescriptionIsReadByWhoeverReadsWorkspaces()
    {
        DescriptionTypes.ByName("Workspace").ReadPermission.ShouldBe("workspaces:read");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void ACalculationsAndAVersionsDescriptionAreReadByWhoeverReadsCalculations()
    {
        DescriptionTypes.ByName("Calculation").ReadPermission.ShouldBe("calculation-entities:read");
        DescriptionTypes.ByName("CalculationVersion").ReadPermission.ShouldBe("calculation-entities:read");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void ADataStoresDescriptionIsReadByWhoeverReadsDataStores()
    {
        DescriptionTypes.ByName("DataStore").ReadPermission.ShouldBe("datastores:read");
        DescriptionTypes.ByName("DataPath").ReadPermission.ShouldBe("datastores:read");
        DescriptionTypes.ByName("DataContainer").ReadPermission.ShouldBe("datastores:read");
        DescriptionTypes.ByName("DataContainerField").ReadPermission.ShouldBe("datastores:read");
        DescriptionTypes.ByName("DataContainerKey").ReadPermission.ShouldBe("datastores:read");
        DescriptionTypes.ByName("DataConfigurationRule").ReadPermission.ShouldBe("datastores:read");
        DescriptionTypes.ByName("DataConfigurationRuleValue").ReadPermission.ShouldBe("datastores:read");
        DescriptionTypes.ByName("DataSetAnnotation").ReadPermission.ShouldBe("datastores:read");
        DescriptionTypes.ByName("DataSetAnnotationField").ReadPermission.ShouldBe("datastores:read");
        DescriptionTypes.ByName("QualityRule").ReadPermission.ShouldBe("quality/rules:read");
        DescriptionTypes.ByName("QualityRuleReferenceValue").ReadPermission.ShouldBe("quality/rules:read");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void ACategorysDescriptionIsReadByWhoeverReadsCategories()
    {
        DescriptionTypes.ByName("CatalogCategory").ReadPermission.ShouldBe("categories:read");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AnEscalationPolicyAndAThresholdDescriptionIsReadByWhoeverReadsThatThing()
    {
        DescriptionTypes.ByName("EscalationPolicy").ReadPermission.ShouldBe("escalation:read");
        DescriptionTypes.ByName("Threshold").ReadPermission.ShouldBe("thresholds:read");
        DescriptionTypes.ByName("EscalationPolicy").HasOwner.ShouldBeFalse();
        DescriptionTypes.ByName("Threshold").HasOwner.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void APipelineParameterScriptWebhookAndNodeDescriptionIsReadByWhoeverReadsThatThing()
    {
        DescriptionTypes.ByName("Pipeline").ReadPermission.ShouldBe("pipelines:read");
        DescriptionTypes.ByName("PipelineParameter").ReadPermission.ShouldBe("pipelines:read");
        DescriptionTypes.ByName("OrchestrationNode").ReadPermission.ShouldBe("projects:read");
        DescriptionTypes.ByName("SqlScript").ReadPermission.ShouldBe("scripts:read");
        DescriptionTypes.ByName("WebhookTrigger").ReadPermission.ShouldBe("webhooks:read");
        DescriptionTypes.ByName("Pipeline").HasOwner.ShouldBeFalse();
        DescriptionTypes.ByName("PipelineParameter").HasOwner.ShouldBeTrue();
        DescriptionTypes.ByName("OrchestrationNode").HasOwner.ShouldBeFalse();
        DescriptionTypes.ByName("SqlScript").HasOwner.ShouldBeFalse();
        DescriptionTypes.ByName("WebhookTrigger").HasOwner.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void ANotificationRuleListAndMessagingDescriptionIsReadByWhoeverReadsThatThing()
    {
        DescriptionTypes.ByName("Notification").ReadPermission.ShouldBe("notifications:read");
        DescriptionTypes.ByName("NotificationRule").ReadPermission.ShouldBe("notifications/rules:read");
        DescriptionTypes.ByName("NotificationList").ReadPermission.ShouldBe("notifications/lists:read");
        DescriptionTypes.ByName("Messaging").ReadPermission.ShouldBe("messages:read");
        DescriptionTypes.ByName("Identity").ReadPermission.ShouldBe("identities:read");
        DescriptionTypes.ByName("SecretManager").ReadPermission.ShouldBe("system:admin");
        DescriptionTypes.ByName("Notification").HasOwner.ShouldBeFalse();
        DescriptionTypes.ByName("NotificationRule").HasOwner.ShouldBeFalse();
        DescriptionTypes.ByName("NotificationList").HasOwner.ShouldBeFalse();
        DescriptionTypes.ByName("Messaging").HasOwner.ShouldBeFalse();
        DescriptionTypes.ByName("Identity").HasOwner.ShouldBeFalse();
        DescriptionTypes.ByName("SecretManager").HasOwner.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void TheOneShippedSubTypeIsTheTextOfTheThingsOwnDescription()
    {
        DescriptionSubTypes.All().Select(subType => subType.Name).ToArray().ShouldBe(["Text"]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void KeyWritesItsFourPartsAsTheRowName()
    {
        new DescriptionKey("DataSet", "Text", System.Guid.Parse("00000000-0000-0000-0000-0000000000a1"), "en-US").AsName()
            .ShouldBe("DataSet/Text/00000000-0000-0000-0000-0000000000a1/en-US");
    }
}
