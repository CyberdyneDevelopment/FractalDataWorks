using System;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Fdw.Results;

namespace Fdw.Services.Messaging;

/// <summary>
/// Default messaging service type that registers <see cref="IMessageService"/>, <see cref="IAccessRequestService"/>
/// <see cref="IUiFeedbackService"/>, <see cref="IAlertActionService"/> and <see cref="IAlertKindSettingsService"/> with the dependency injection container.
/// </summary>
[ExcludeFromCodeCoverage]
[Implementation(typeof(MessagingServiceTypes), "Default")]
public sealed class DefaultMessagingServiceType : MessagingServiceTypeBase
{
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

}
