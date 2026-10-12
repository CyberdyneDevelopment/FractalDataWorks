// Auto-generated resource accessor for Strings.resx
// This provides strongly-typed access to localized resource strings

using System.Resources;
using System.Runtime.CompilerServices;

namespace Fdw.MessageLogging.Generators
{
    internal static class SR
    {
        private static ResourceManager? s_resourceManager;
        internal static ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new ResourceManager(typeof(FxResources.Fdw.MessageLogging.Generators.SR)));

        internal static string InvalidLoggingMethodNameMessage => GetResourceString("InvalidLoggingMethodNameMessage");
        internal static string ShouldntMentionLogLevelInMessageTitle => GetResourceString("ShouldntMentionLogLevelInMessageTitle");
        internal static string ShouldntMentionInTemplateMessage => GetResourceString("ShouldntMentionInTemplateMessage");
        internal static string InvalidLoggingMethodParameterNameMessage => GetResourceString("InvalidLoggingMethodParameterNameMessage");
        internal static string MissingRequiredTypeTitle => GetResourceString("MissingRequiredTypeTitle");
        internal static string MissingRequiredTypeMessage => GetResourceString("MissingRequiredTypeMessage");
        internal static string ShouldntReuseEventIdsTitle => GetResourceString("ShouldntReuseEventIdsTitle");
        internal static string ShouldntReuseEventIdsMessage => GetResourceString("ShouldntReuseEventIdsMessage");
        internal static string LoggingMethodMustReturnVoidTitle => GetResourceString("LoggingMethodMustReturnVoidTitle");
        internal static string LoggingMethodMustReturnVoidMessage => GetResourceString("LoggingMethodMustReturnVoidMessage");
        internal static string MissingLoggerArgumentTitle => GetResourceString("MissingLoggerArgumentTitle");
        internal static string MissingLoggerArgumentMessage => GetResourceString("MissingLoggerArgumentMessage");
        internal static string LoggingMethodShouldBeStaticTitle => GetResourceString("LoggingMethodShouldBeStaticTitle");
        internal static string LoggingMethodShouldBeStaticMessage => GetResourceString("LoggingMethodShouldBeStaticMessage");
        internal static string LoggingMethodMustBePartialTitle => GetResourceString("LoggingMethodMustBePartialTitle");
        internal static string LoggingMethodMustBePartialMessage => GetResourceString("LoggingMethodMustBePartialMessage");
        internal static string LoggingMethodIsGenericTitle => GetResourceString("LoggingMethodIsGenericTitle");
        internal static string LoggingMethodIsGenericMessage => GetResourceString("LoggingMethodIsGenericMessage");
        internal static string RedundantQualifierInMessageTitle => GetResourceString("RedundantQualifierInMessageTitle");
        internal static string RedundantQualifierInMessageMessage => GetResourceString("RedundantQualifierInMessageMessage");
        internal static string ShouldntMentionExceptionInMessageTitle => GetResourceString("ShouldntMentionExceptionInMessageTitle");
        internal static string ShouldntMentionExceptionInMessageMessage => GetResourceString("ShouldntMentionExceptionInMessageMessage");
        internal static string TemplateHasNoCorrespondingArgumentTitle => GetResourceString("TemplateHasNoCorrespondingArgumentTitle");
        internal static string TemplateHasNoCorrespondingArgumentMessage => GetResourceString("TemplateHasNoCorrespondingArgumentMessage");
        internal static string ArgumentHasNoCorrespondingTemplateTitle => GetResourceString("ArgumentHasNoCorrespondingTemplateTitle");
        internal static string ArgumentHasNoCorrespondingTemplateMessage => GetResourceString("ArgumentHasNoCorrespondingTemplateMessage");
        internal static string LoggingMethodHasBodyTitle => GetResourceString("LoggingMethodHasBodyTitle");
        internal static string LoggingMethodHasBodyMessage => GetResourceString("LoggingMethodHasBodyMessage");
        internal static string MissingLogLevelTitle => GetResourceString("MissingLogLevelTitle");
        internal static string MissingLogLevelMessage => GetResourceString("MissingLogLevelMessage");
        internal static string ShouldntMentionLoggerInMessageTitle => GetResourceString("ShouldntMentionLoggerInMessageTitle");
        internal static string ShouldntMentionLoggerInMessageMessage => GetResourceString("ShouldntMentionLoggerInMessageMessage");
        internal static string MissingLoggerFieldTitle => GetResourceString("MissingLoggerFieldTitle");
        internal static string MissingLoggerFieldMessage => GetResourceString("MissingLoggerFieldMessage");
        internal static string MultipleLoggerFieldsTitle => GetResourceString("MultipleLoggerFieldsTitle");
        internal static string MultipleLoggerFieldsMessage => GetResourceString("MultipleLoggerFieldsMessage");
        internal static string InconsistentTemplateCasingTitle => GetResourceString("InconsistentTemplateCasingTitle");
        internal static string InconsistentTemplateCasingMessage => GetResourceString("InconsistentTemplateCasingMessage");
        internal static string MalformedFormatStringsTitle => GetResourceString("MalformedFormatStringsTitle");
        internal static string MalformedFormatStringsMessage => GetResourceString("MalformedFormatStringsMessage");
        internal static string GeneratingForMax6ArgumentsTitle => GetResourceString("GeneratingForMax6ArgumentsTitle");
        internal static string GeneratingForMax6ArgumentsMessage => GetResourceString("GeneratingForMax6ArgumentsMessage");
        internal static string InvalidLoggingMethodParameterOutTitle => GetResourceString("InvalidLoggingMethodParameterOutTitle");
        internal static string InvalidLoggingMethodParameterOutMessage => GetResourceString("InvalidLoggingMethodParameterOutMessage");
        internal static string ShouldntReuseEventNamesTitle => GetResourceString("ShouldntReuseEventNamesTitle");
        internal static string ShouldntReuseEventNamesMessage => GetResourceString("ShouldntReuseEventNamesMessage");
        internal static string LoggingUnsupportedLanguageVersionTitle => GetResourceString("LoggingUnsupportedLanguageVersionTitle");
        internal static string LoggingUnsupportedLanguageVersionMessage => GetResourceString("LoggingUnsupportedLanguageVersionMessage");
        internal static string PrimaryConstructorParameterLoggerHiddenTitle => GetResourceString("PrimaryConstructorParameterLoggerHiddenTitle");
        internal static string PrimaryConstructorParameterLoggerHiddenMessage => GetResourceString("PrimaryConstructorParameterLoggerHiddenMessage");
        internal static string LoggingUnsupportedLanguageVersionMessageFormat => GetResourceString("LoggingUnsupportedLanguageVersionMessage");
        internal static string InvalidTypeCodeTitle => GetResourceString("InvalidTypeCodeTitle");
        internal static string InvalidTypeCodeMessage => GetResourceString("InvalidTypeCodeMessage");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string GetResourceString(string resourceKey, string? defaultValue = null) =>
            ResourceManager.GetString(resourceKey, System.Globalization.CultureInfo.InvariantCulture) ?? defaultValue ?? resourceKey;
    }
}

namespace FxResources.Fdw.MessageLogging.Generators
{
    internal static class SR { }
}
