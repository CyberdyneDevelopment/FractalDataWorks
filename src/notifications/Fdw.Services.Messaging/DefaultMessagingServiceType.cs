using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Fdw.Results;
using Fdw.ServiceTypes;

namespace Fdw.Services.Messaging;

/// <summary>
/// Default messaging service type that registers <see cref="IMessageService"/>, <see cref="IAccessRequestService"/>
/// <see cref="IUiFeedbackService"/>, <see cref="IAlertActionService"/> and <see cref="IAlertKindSettingsService"/> with the dependency injection container.
/// </summary>
/// <remarks>
/// The limits a UI comment and a question about alerts are held to are configuration (FDW-7058, FDW-7059), on FDW's declared-defaults mechanism
/// (FDW-3823, FDW-7103): the defaults are this option's <see cref="DeclaredDefaults"/>, a host changes one before Register with
/// <c>ServiceTypeDefaults.Override(option, "MaxPictureBytes", value)</c>, and a value the host's configuration states wins: under
/// <see cref="UiFeedbackSection"/> (<c>MaxBodyLength</c>, <c>MaxPictureBytes</c>, <c>MaxAnchors</c>, <c>MaxAnchorLength</c>, <c>MaxPageSize</c>) and
/// <see cref="AlertsSection"/> (<c>MaxAlerts</c>). A value that is not a whole number of at least one refuses registration and names the key
/// (<c>DeclaredSetting.ConfiguredCount</c>). No code caps a comment by a number written anywhere else.
/// </remarks>
[ExcludeFromCodeCoverage]
[Implementation(typeof(MessagingServiceTypes), "Default")]
public sealed class DefaultMessagingServiceType : MessagingServiceTypeBase
{
    /// <summary>The section a host states the UI comment limits in.</summary>
    public const string UiFeedbackSection = "Messaging:Default:UiFeedback";

    /// <summary>The section a host states the alert limits in (FDW-7059).</summary>
    public const string AlertsSection = "Messaging:Default:Alerts";

    // The limits' defaults, by the key a host states them under. The note columns are NVARCHAR(MAX) (FDW-7101), so any note length is stored whole.
    // 16 MiB of picture holds any window the console draws at pixel ratio 1, and its base64 (about 22.4 million characters) still fits Kestrel's default
    // 30,000,000-byte request body; a host that states more than about 21 MiB also raises Kestrel:Limits:MaxRequestBodySize.
    private static readonly IReadOnlyDictionary<string, object> Defaults = new Dictionary<string, object>(StringComparer.Ordinal)
    {
        [nameof(UiFeedbackLimits.MaxBodyLength)] = 4000,
        [nameof(UiFeedbackLimits.MaxPictureBytes)] = 16 * 1024 * 1024,
        [nameof(UiFeedbackLimits.MaxAnchors)] = 20,
        [nameof(UiFeedbackLimits.MaxAnchorLength)] = 4000,
        [nameof(UiFeedbackLimits.MaxPageSize)] = 200,
        [nameof(AlertLimits.MaxAlerts)] = 100,
    };

    /// <inheritdoc />
    public override IReadOnlyDictionary<string, object> DeclaredDefaults => Defaults;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultMessagingServiceType"/> class.
    /// </summary>
    public DefaultMessagingServiceType()
        : base(
            "Default",
            "Messaging:Default",
            "Default Messaging",
            "Default messaging services with message, access request and UI comment support")
    {
        Registration((builder, loggerFactory) =>
        {
            var logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<DefaultMessagingServiceType>();
            var limits = UiFeedbackLimitsOf(builder, logger);
            if (!limits.IsSuccess || limits.Value is null)
                return limits.ToNewResult<IHostApplicationBuilder>();

            var alertMax = DeclaredSetting.ConfiguredCount(builder, AlertsSection, this, nameof(AlertLimits.MaxAlerts), logger);
            if (!alertMax.IsSuccess)
                return alertMax.ToNewResult<IHostApplicationBuilder>();

            builder.Services.TryAddSingleton(limits.Value);
            builder.Services.TryAddSingleton(new AlertLimits(alertMax.Value));
            builder.Services.AddScoped<MessageService>();
            builder.Services.AddScoped<IMessageService>(services => services.GetRequiredService<MessageService>());
            builder.Services.AddScoped<IAlertSource>(services => services.GetRequiredService<MessageService>());
            builder.Services.AddScoped<IAccessRequestService, AccessRequestService>();
            builder.Services.TryAddSingleton(TimeProvider.System);
            builder.Services.AddScoped<UiFeedbackService>();
            builder.Services.AddScoped<IUiFeedbackService>(services => services.GetRequiredService<UiFeedbackService>());
            builder.Services.AddScoped<IAlertSource>(services => services.GetRequiredService<UiFeedbackService>());
            builder.Services.AddScoped<IAlertSource, ScheduleFailureAlertSource>();
            builder.Services.AddScoped<IAlertActionService, AlertActionService>();
            builder.Services.AddScoped<IAlertKindSettingsService, AlertKindSettingsService>();
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

    }

    // The limits that apply: each the value the host's configuration states, else the host's override, else this option's declared default.
    private IGenericResult<UiFeedbackLimits> UiFeedbackLimitsOf(IHostApplicationBuilder builder, ILogger logger)
    {
        var body = DeclaredSetting.ConfiguredCount(builder, UiFeedbackSection, this, nameof(UiFeedbackLimits.MaxBodyLength), logger);
        var picture = DeclaredSetting.ConfiguredCount(builder, UiFeedbackSection, this, nameof(UiFeedbackLimits.MaxPictureBytes), logger);
        var anchors = DeclaredSetting.ConfiguredCount(builder, UiFeedbackSection, this, nameof(UiFeedbackLimits.MaxAnchors), logger);
        var anchorLength = DeclaredSetting.ConfiguredCount(builder, UiFeedbackSection, this, nameof(UiFeedbackLimits.MaxAnchorLength), logger);
        var page = DeclaredSetting.ConfiguredCount(builder, UiFeedbackSection, this, nameof(UiFeedbackLimits.MaxPageSize), logger);

        // Why one check per result: each failure is passed up as it is, and FDW016 reads the check on the result it guards.
        if (!body.IsSuccess)
            return body.ToNewResult<UiFeedbackLimits>();

        if (!picture.IsSuccess)
            return picture.ToNewResult<UiFeedbackLimits>();

        if (!anchors.IsSuccess)
            return anchors.ToNewResult<UiFeedbackLimits>();

        if (!anchorLength.IsSuccess)
            return anchorLength.ToNewResult<UiFeedbackLimits>();

        if (!page.IsSuccess)
            return page.ToNewResult<UiFeedbackLimits>();

        return GenericResult<UiFeedbackLimits>.Success(new UiFeedbackLimits(body.Value, picture.Value, anchors.Value, anchorLength.Value, page.Value));
    }
}
