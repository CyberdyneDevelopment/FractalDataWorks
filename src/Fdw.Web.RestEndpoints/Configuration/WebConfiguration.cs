using System;
using System.Diagnostics.CodeAnalysis;
// using Fdw.Web.Http.Abstractions.Base;
using Fdw.Configuration.Abstractions;

namespace Fdw.Web.RestEndpoints.Configuration;

/// <summary>
/// Main web configuration implementation for the Fdw Web Framework.
/// Provides concrete configuration with validation rules for REST-based web applications.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class WebConfiguration : ConfigurationBase<WebConfiguration>
{
    /// <inheritdoc/>
    public override string ServiceType => "Web";

    /// <inheritdoc/>
    public override string SectionName => "FdwWeb";

    /// <summary>
    /// Gets or sets the host address to bind the web server to.
    /// </summary>
    public string Host { get; init; } = "localhost";

    /// <summary>
    /// Gets or sets the port number to bind the web server to.
    /// </summary>
    public int Port { get; init; } = 5000;

    /// <summary>
    /// Gets or sets a value indicating whether HTTPS should be enforced.
    /// </summary>
    public bool ForceHttps { get; init; } = true;

    /// <summary>
    /// Gets or sets the path to the SSL certificate file.
    /// Required when ForceHttps is true in production environments.
    /// </summary>
    public string? SslCertificatePath { get; init; }

    /// <summary>
    /// Gets or sets the password for the SSL certificate.
    /// </summary>
    public string? SslCertificatePassword { get; init; }

    /// <summary>
    /// Gets or sets the authentication configuration.
    /// </summary>
    public AuthenticationConfiguration Authentication { get; init; } = new();

    /// <summary>
    /// Gets or sets the CORS configuration.
    /// </summary>
    public CorsConfiguration Cors { get; init; } = new();

    /// <summary>
    /// Gets or sets the Swagger/OpenAPI configuration.
    /// </summary>
    public SwaggerConfiguration Swagger { get; init; } = new();

    // /// <inheritdoc/>
    // protected override void ConfigureValidation(AbstractValidator<WebConfiguration> validator)
    // {
    //     // Call base validation first
    //     base.ConfigureValidation(validator);
    //     
    //     // Host validation
    //     validator.RuleFor(x => x.Host)
    //         .NotEmpty()
    //         .WithMessage("Host is required");

    //     // Port validation
    //     validator.RuleFor(x => x.Port)
    //         .InclusiveBetween(1, 65535)
    //         .WithMessage("Port must be between 1 and 65535");

    //     // SSL certificate validation when HTTPS is enforced
    //     validator.RuleFor(x => x.SslCertificatePath)
    //         .NotEmpty()
    //         .When(x => x.ForceHttps)
    //         .WithMessage("SSL certificate path is required when ForceHttps is enabled");

    //     // Authentication configuration validation
    //     validator.RuleFor(x => x.Authentication)
    //         .NotNull()
    //         .WithMessage("Authentication configuration is required")
    //         .DependentRules(() =>
    //         {
    //             validator.RuleFor(x => x.Authentication.Jwt.Issuer)
    //                 .NotEmpty()
    //                 .When(x => !string.IsNullOrEmpty(x.Authentication.Jwt.SecretKey))
    //                 .WithMessage("JWT issuer is required when JWT is configured");

    //             validator.RuleFor(x => x.Authentication.Jwt.Audience)
    //                 .NotEmpty()
    //                 .When(x => !string.IsNullOrEmpty(x.Authentication.Jwt.SecretKey))
    //                 .WithMessage("JWT audience is required when JWT is configured");

    //             validator.RuleFor(x => x.Authentication.Jwt.SecretKey)
    //                 .MinimumLength(32)
    //                 .When(x => !string.IsNullOrEmpty(x.Authentication.Jwt.SecretKey))
    //                 .WithMessage("JWT secret key must be at least 32 characters long");

    //             validator.RuleFor(x => x.Authentication.Jwt.ExpirationMinutes)
    //                 .GreaterThan(0)
    //                 .WithMessage("JWT expiration must be positive");
    //         });
    // }

    // /// <inheritdoc/>
    // protected override void CopyTo(WebConfiguration target)
    // {
    //     base.CopyTo(target);
    //     
    //     if (target != null)
    //     {
    //         // Copy web-specific properties via reflection since they're init-only
    //         var sourceType = typeof(WebConfiguration);
    //         var properties = new[] 
    //         {
    //             nameof(Host),
    //             nameof(Port),
    //             nameof(ForceHttps),
    //             nameof(SslCertificatePath),
    //             nameof(SslCertificatePassword)
    //         };

    //         foreach (var propertyName in properties)
    //         {
    //             var sourceProperty = sourceType.GetProperty(propertyName);
    //             var targetProperty = sourceType.GetProperty(propertyName);
    //             
    //             if (sourceProperty != null && targetProperty?.SetMethod != null)
    //             {
    //                 var value = sourceProperty.GetValue(this);
    //                 targetProperty.SetValue(target, value);
    //             }
    //         }
    //     }
    // }

    // /// <inheritdoc/>
    // protected override IValidator<WebConfiguration>? GetValidator()
    // {
    //     return new WebConfigurationValidator();
    // }
}