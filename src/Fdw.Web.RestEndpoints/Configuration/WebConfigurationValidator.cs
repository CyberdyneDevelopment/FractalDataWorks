using FluentValidation;

namespace Fdw.Web.RestEndpoints.Configuration;

/// <summary>
/// FluentValidation validator for WebConfiguration.
/// </summary>
public sealed class WebConfigurationValidator : AbstractValidator<WebConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WebConfigurationValidator"/> class.
    /// </summary>
    public WebConfigurationValidator()
    {
        // // Base URL validation
        // RuleFor(x => x.BaseUrl)
        //     .NotEmpty().WithMessage("Base URL is required")
        //     .Must(BeAValidUrl).WithMessage("Base URL must be a valid URL");

        // Host validation
        RuleFor(x => x.Host)
            .NotEmpty().WithMessage("Host is required");

        // Port validation
        RuleFor(x => x.Port)
            .GreaterThan(0).WithMessage("Port must be greater than 0")
            .LessThanOrEqualTo(65535).WithMessage("Port must be less than or equal to 65535");

        // Add any additional REST-specific validation here if needed
    }

    private static bool BeAValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out _);
    }
}