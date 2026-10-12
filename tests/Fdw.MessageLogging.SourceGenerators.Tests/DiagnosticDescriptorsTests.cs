using Microsoft.CodeAnalysis;
using Shouldly;
using Xunit;

namespace Fdw.MessageLogging.Generators.Tests;

public sealed class DiagnosticDescriptorsTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void InvalidLoggingMethodNameHasCorrectProperties()
    {
        var descriptor = DiagnosticDescriptors.InvalidLoggingMethodName;

        descriptor.Id.ShouldBe("SYSLIB1001");
        descriptor.Category.ShouldBe("LoggingGenerator");
        descriptor.DefaultSeverity.ShouldBe(DiagnosticSeverity.Error);
        descriptor.IsEnabledByDefault.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void ShouldntMentionLogLevelInMessageHasCorrectProperties()
    {
        var descriptor = DiagnosticDescriptors.ShouldntMentionLogLevelInMessage;

        descriptor.Id.ShouldBe("SYSLIB1002");
        descriptor.Category.ShouldBe("LoggingGenerator");
        descriptor.DefaultSeverity.ShouldBe(DiagnosticSeverity.Warning);
        descriptor.IsEnabledByDefault.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void InvalidLoggingMethodParameterNameHasCorrectProperties()
    {
        var descriptor = DiagnosticDescriptors.InvalidLoggingMethodParameterName;

        descriptor.Id.ShouldBe("SYSLIB1003");
        descriptor.Category.ShouldBe("LoggingGenerator");
        descriptor.DefaultSeverity.ShouldBe(DiagnosticSeverity.Error);
        descriptor.IsEnabledByDefault.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void MissingRequiredTypeHasCorrectProperties()
    {
        var descriptor = DiagnosticDescriptors.MissingRequiredType;

        descriptor.Id.ShouldBe("SYSLIB1005");
        descriptor.Category.ShouldBe("LoggingGenerator");
        descriptor.DefaultSeverity.ShouldBe(DiagnosticSeverity.Error);
        descriptor.IsEnabledByDefault.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void ShouldntReuseEventIdsHasCorrectProperties()
    {
        var descriptor = DiagnosticDescriptors.ShouldntReuseEventIds;

        descriptor.Id.ShouldBe("SYSLIB1006");
        descriptor.Category.ShouldBe("LoggingGenerator");
        descriptor.DefaultSeverity.ShouldBe(DiagnosticSeverity.Info);
        descriptor.IsEnabledByDefault.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void LoggingMethodMustReturnVoidHasCorrectProperties()
    {
        var descriptor = DiagnosticDescriptors.LoggingMethodMustReturnVoid;

        descriptor.Id.ShouldBe("SYSLIB1007");
        descriptor.Category.ShouldBe("LoggingGenerator");
        descriptor.DefaultSeverity.ShouldBe(DiagnosticSeverity.Error);
        descriptor.IsEnabledByDefault.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void MissingLoggerArgumentHasCorrectProperties()
    {
        var descriptor = DiagnosticDescriptors.MissingLoggerArgument;

        descriptor.Id.ShouldBe("SYSLIB1008");
        descriptor.Category.ShouldBe("LoggingGenerator");
        descriptor.DefaultSeverity.ShouldBe(DiagnosticSeverity.Error);
        descriptor.IsEnabledByDefault.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void LoggingMethodShouldBeStaticHasCorrectProperties()
    {
        var descriptor = DiagnosticDescriptors.LoggingMethodShouldBeStatic;

        descriptor.Id.ShouldBe("SYSLIB1009");
        descriptor.Category.ShouldBe("LoggingGenerator");
        descriptor.DefaultSeverity.ShouldBe(DiagnosticSeverity.Warning);
        descriptor.IsEnabledByDefault.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void LoggingMethodMustBePartialHasCorrectProperties()
    {
        var descriptor = DiagnosticDescriptors.LoggingMethodMustBePartial;

        descriptor.Id.ShouldBe("SYSLIB1010");
        descriptor.Category.ShouldBe("LoggingGenerator");
        descriptor.DefaultSeverity.ShouldBe(DiagnosticSeverity.Error);
        descriptor.IsEnabledByDefault.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void LoggingMethodIsGenericHasCorrectProperties()
    {
        var descriptor = DiagnosticDescriptors.LoggingMethodIsGeneric;

        descriptor.Id.ShouldBe("SYSLIB1011");
        descriptor.Category.ShouldBe("LoggingGenerator");
        descriptor.DefaultSeverity.ShouldBe(DiagnosticSeverity.Error);
        descriptor.IsEnabledByDefault.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void RedundantQualifierInMessageHasCorrectProperties()
    {
        var descriptor = DiagnosticDescriptors.RedundantQualifierInMessage;

        descriptor.Id.ShouldBe("SYSLIB1012");
        descriptor.Category.ShouldBe("LoggingGenerator");
        descriptor.DefaultSeverity.ShouldBe(DiagnosticSeverity.Warning);
        descriptor.IsEnabledByDefault.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void ShouldntMentionExceptionInMessageHasCorrectProperties()
    {
        var descriptor = DiagnosticDescriptors.ShouldntMentionExceptionInMessage;

        descriptor.Id.ShouldBe("SYSLIB1013");
        descriptor.Category.ShouldBe("LoggingGenerator");
        descriptor.DefaultSeverity.ShouldBe(DiagnosticSeverity.Warning);
        descriptor.IsEnabledByDefault.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void TemplateHasNoCorrespondingArgumentHasCorrectProperties()
    {
        var descriptor = DiagnosticDescriptors.TemplateHasNoCorrespondingArgument;

        descriptor.Id.ShouldBe("SYSLIB1014");
        descriptor.Category.ShouldBe("LoggingGenerator");
        descriptor.DefaultSeverity.ShouldBe(DiagnosticSeverity.Error);
        descriptor.IsEnabledByDefault.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void ArgumentHasNoCorrespondingTemplateHasCorrectProperties()
    {
        var descriptor = DiagnosticDescriptors.ArgumentHasNoCorrespondingTemplate;

        descriptor.Id.ShouldBe("SYSLIB1015");
        descriptor.Category.ShouldBe("LoggingGenerator");
        descriptor.DefaultSeverity.ShouldBe(DiagnosticSeverity.Warning);
        descriptor.IsEnabledByDefault.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void LoggingMethodHasBodyHasCorrectProperties()
    {
        var descriptor = DiagnosticDescriptors.LoggingMethodHasBody;

        descriptor.Id.ShouldBe("SYSLIB1016");
        descriptor.Category.ShouldBe("LoggingGenerator");
        descriptor.DefaultSeverity.ShouldBe(DiagnosticSeverity.Error);
        descriptor.IsEnabledByDefault.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void MissingLogLevelHasCorrectProperties()
    {
        var descriptor = DiagnosticDescriptors.MissingLogLevel;

        descriptor.Id.ShouldBe("SYSLIB1017");
        descriptor.Category.ShouldBe("LoggingGenerator");
        descriptor.DefaultSeverity.ShouldBe(DiagnosticSeverity.Error);
        descriptor.IsEnabledByDefault.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void ShouldntMentionLoggerInMessageHasCorrectProperties()
    {
        var descriptor = DiagnosticDescriptors.ShouldntMentionLoggerInMessage;

        descriptor.Id.ShouldBe("SYSLIB1018");
        descriptor.Category.ShouldBe("LoggingGenerator");
        descriptor.DefaultSeverity.ShouldBe(DiagnosticSeverity.Warning);
        descriptor.IsEnabledByDefault.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void MissingLoggerFieldHasCorrectProperties()
    {
        var descriptor = DiagnosticDescriptors.MissingLoggerField;

        descriptor.Id.ShouldBe("SYSLIB1019");
        descriptor.Category.ShouldBe("LoggingGenerator");
        descriptor.DefaultSeverity.ShouldBe(DiagnosticSeverity.Error);
        descriptor.IsEnabledByDefault.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void MultipleLoggerFieldsHasCorrectProperties()
    {
        var descriptor = DiagnosticDescriptors.MultipleLoggerFields;

        descriptor.Id.ShouldBe("SYSLIB1020");
        descriptor.Category.ShouldBe("LoggingGenerator");
        descriptor.DefaultSeverity.ShouldBe(DiagnosticSeverity.Error);
        descriptor.IsEnabledByDefault.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void InconsistentTemplateCasingHasCorrectProperties()
    {
        var descriptor = DiagnosticDescriptors.InconsistentTemplateCasing;

        descriptor.Id.ShouldBe("SYSLIB1021");
        descriptor.Category.ShouldBe("LoggingGenerator");
        descriptor.DefaultSeverity.ShouldBe(DiagnosticSeverity.Error);
        descriptor.IsEnabledByDefault.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void MalformedFormatStringsHasCorrectProperties()
    {
        var descriptor = DiagnosticDescriptors.MalformedFormatStrings;

        descriptor.Id.ShouldBe("SYSLIB1022");
        descriptor.Category.ShouldBe("LoggingGenerator");
        descriptor.DefaultSeverity.ShouldBe(DiagnosticSeverity.Error);
        descriptor.IsEnabledByDefault.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GeneratingForMax6ArgumentsHasCorrectProperties()
    {
        var descriptor = DiagnosticDescriptors.GeneratingForMax6Arguments;

        descriptor.Id.ShouldBe("SYSLIB1023");
        descriptor.Category.ShouldBe("LoggingGenerator");
        descriptor.DefaultSeverity.ShouldBe(DiagnosticSeverity.Error);
        descriptor.IsEnabledByDefault.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void InvalidLoggingMethodParameterOutHasCorrectProperties()
    {
        var descriptor = DiagnosticDescriptors.InvalidLoggingMethodParameterOut;

        descriptor.Id.ShouldBe("SYSLIB1024");
        descriptor.Category.ShouldBe("LoggingGenerator");
        descriptor.DefaultSeverity.ShouldBe(DiagnosticSeverity.Error);
        descriptor.IsEnabledByDefault.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void ShouldntReuseEventNamesHasCorrectProperties()
    {
        var descriptor = DiagnosticDescriptors.ShouldntReuseEventNames;

        descriptor.Id.ShouldBe("SYSLIB1025");
        descriptor.Category.ShouldBe("LoggingGenerator");
        descriptor.DefaultSeverity.ShouldBe(DiagnosticSeverity.Warning);
        descriptor.IsEnabledByDefault.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void LoggingUnsupportedLanguageVersionHasCorrectProperties()
    {
        var descriptor = DiagnosticDescriptors.LoggingUnsupportedLanguageVersion;

        descriptor.Id.ShouldBe("SYSLIB1026");
        descriptor.Category.ShouldBe("LoggingGenerator");
        descriptor.DefaultSeverity.ShouldBe(DiagnosticSeverity.Error);
        descriptor.IsEnabledByDefault.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void PrimaryConstructorParameterLoggerHiddenHasCorrectProperties()
    {
        var descriptor = DiagnosticDescriptors.PrimaryConstructorParameterLoggerHidden;

        descriptor.Id.ShouldBe("SYSLIB1027");
        descriptor.Category.ShouldBe("LoggingGenerator");
        descriptor.DefaultSeverity.ShouldBe(DiagnosticSeverity.Info);
        descriptor.IsEnabledByDefault.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void AllDiagnosticDescriptorsHaveUniqueIds()
    {
        var descriptors = new[]
        {
            DiagnosticDescriptors.InvalidLoggingMethodName,
            DiagnosticDescriptors.ShouldntMentionLogLevelInMessage,
            DiagnosticDescriptors.InvalidLoggingMethodParameterName,
            DiagnosticDescriptors.MissingRequiredType,
            DiagnosticDescriptors.ShouldntReuseEventIds,
            DiagnosticDescriptors.LoggingMethodMustReturnVoid,
            DiagnosticDescriptors.MissingLoggerArgument,
            DiagnosticDescriptors.LoggingMethodShouldBeStatic,
            DiagnosticDescriptors.LoggingMethodMustBePartial,
            DiagnosticDescriptors.LoggingMethodIsGeneric,
            DiagnosticDescriptors.RedundantQualifierInMessage,
            DiagnosticDescriptors.ShouldntMentionExceptionInMessage,
            DiagnosticDescriptors.TemplateHasNoCorrespondingArgument,
            DiagnosticDescriptors.ArgumentHasNoCorrespondingTemplate,
            DiagnosticDescriptors.LoggingMethodHasBody,
            DiagnosticDescriptors.MissingLogLevel,
            DiagnosticDescriptors.ShouldntMentionLoggerInMessage,
            DiagnosticDescriptors.MissingLoggerField,
            DiagnosticDescriptors.MultipleLoggerFields,
            DiagnosticDescriptors.InconsistentTemplateCasing,
            DiagnosticDescriptors.MalformedFormatStrings,
            DiagnosticDescriptors.GeneratingForMax6Arguments,
            DiagnosticDescriptors.InvalidLoggingMethodParameterOut,
            DiagnosticDescriptors.ShouldntReuseEventNames,
            DiagnosticDescriptors.LoggingUnsupportedLanguageVersion,
            DiagnosticDescriptors.PrimaryConstructorParameterLoggerHidden
        };

        var ids = descriptors.Select(d => d.Id).ToList();
        var distinctIds = ids.Distinct().ToList();

        ids.Count.ShouldBe(distinctIds.Count);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void AllDiagnosticDescriptorsAreInLoggingGeneratorCategory()
    {
        var descriptors = new[]
        {
            DiagnosticDescriptors.InvalidLoggingMethodName,
            DiagnosticDescriptors.ShouldntMentionLogLevelInMessage,
            DiagnosticDescriptors.InvalidLoggingMethodParameterName,
            DiagnosticDescriptors.MissingRequiredType,
            DiagnosticDescriptors.ShouldntReuseEventIds,
            DiagnosticDescriptors.LoggingMethodMustReturnVoid,
            DiagnosticDescriptors.MissingLoggerArgument,
            DiagnosticDescriptors.LoggingMethodShouldBeStatic,
            DiagnosticDescriptors.LoggingMethodMustBePartial,
            DiagnosticDescriptors.LoggingMethodIsGeneric,
            DiagnosticDescriptors.RedundantQualifierInMessage,
            DiagnosticDescriptors.ShouldntMentionExceptionInMessage,
            DiagnosticDescriptors.TemplateHasNoCorrespondingArgument,
            DiagnosticDescriptors.ArgumentHasNoCorrespondingTemplate,
            DiagnosticDescriptors.LoggingMethodHasBody,
            DiagnosticDescriptors.MissingLogLevel,
            DiagnosticDescriptors.ShouldntMentionLoggerInMessage,
            DiagnosticDescriptors.MissingLoggerField,
            DiagnosticDescriptors.MultipleLoggerFields,
            DiagnosticDescriptors.InconsistentTemplateCasing,
            DiagnosticDescriptors.MalformedFormatStrings,
            DiagnosticDescriptors.GeneratingForMax6Arguments,
            DiagnosticDescriptors.InvalidLoggingMethodParameterOut,
            DiagnosticDescriptors.ShouldntReuseEventNames,
            DiagnosticDescriptors.LoggingUnsupportedLanguageVersion,
            DiagnosticDescriptors.PrimaryConstructorParameterLoggerHidden
        };

        foreach (var descriptor in descriptors)
        {
            descriptor.Category.ShouldBe("LoggingGenerator");
        }
    }
}
