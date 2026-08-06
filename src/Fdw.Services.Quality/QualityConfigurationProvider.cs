using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Quality.Commands;
using Fdw.Services.Quality.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Fdw.Services.Quality;

/// <summary>
/// Composite configuration provider for the Quality domain. Wraps five two-arity
/// <see cref="DefaultConfigurationProvider{TConfig,TCommand}"/> instances (Quality, Catalog, Promotion).
/// </summary>
public class QualityConfigurationProvider
{
    /// <summary>
    /// Registers the QualityConfigurationProvider with DI, targeting this domain's own default
    /// location (this class's own constructor default). Pure Phase-1b registration — IOptions
    /// binding from IConfiguration is a Phase-1a concern and lives in the consuming
    /// <c>[ServiceTypeOption].Configure</c>, not here.
    /// </summary>
    private readonly DefaultConfigurationProvider<QualityRuleConfiguration, QualityRuleConfigurationCommand> _qualityRuleProvider;
    private readonly DefaultConfigurationProvider<DataSetAnnotationConfiguration, DataSetAnnotationConfigurationCommand> _annotationProvider;
    private readonly DefaultConfigurationProvider<EnvironmentConfiguration, EnvironmentConfigurationCommand> _environmentProvider;
    private readonly DefaultConfigurationProvider<PromotionRequestConfiguration, PromotionRequestConfigurationCommand> _promotionRequestProvider;
    private readonly DefaultConfigurationProvider<GlossaryTermConfiguration, GlossaryTermConfigurationCommand> _glossaryTermProvider;

    /// <summary>Initializes a new instance of the <see cref="QualityConfigurationProvider"/> class.</summary>
    #pragma warning disable MA0051
    public QualityConfigurationProvider(
        ILogger<QualityConfigurationProvider> logger,
        Lazy<IConfigurationGateway> lazyGateway,
        string dataStoreName = "ConfigurationDb",
        Lazy<ICacheInvalidator?>? invalidator = null)
    #pragma warning restore MA0051
    {
        // Why: ILogger<T> is invariant — the QualityConfigurationProvider logger can't be passed
        // to inner DCP<TConfig,TCommand> constructors. Each DCP falls back to NullLogger internally
        // when logger is null, which is the right behavior for these composed inner providers.
        _ = logger;

        // Why: All quality/catalog providers are cfg-tier — cfg-tier; loaded from ConfigurationDb at runtime.
        _qualityRuleProvider = new DefaultConfigurationProvider<QualityRuleConfiguration, QualityRuleConfigurationCommand>(
            logger: null, lazyGateway, dataStoreName, "quality", invalidator);
        _annotationProvider = new DefaultConfigurationProvider<DataSetAnnotationConfiguration, DataSetAnnotationConfigurationCommand>(
            logger: null, lazyGateway, dataStoreName, "catalog", invalidator);
        _environmentProvider = new DefaultConfigurationProvider<EnvironmentConfiguration, EnvironmentConfigurationCommand>(
            logger: null, lazyGateway, dataStoreName, "quality", invalidator);
        _promotionRequestProvider = new DefaultConfigurationProvider<PromotionRequestConfiguration, PromotionRequestConfigurationCommand>(
            logger: null, lazyGateway, dataStoreName, "quality", invalidator);
        _glossaryTermProvider = new DefaultConfigurationProvider<GlossaryTermConfiguration, GlossaryTermConfigurationCommand>(
            logger: null, lazyGateway, dataStoreName, "catalog", invalidator);
    }

    /// <summary>Gets a quality rule configuration by name.</summary>
    public Task<IGenericResult<QualityRuleConfiguration>> GetQualityRule(string name, CancellationToken cancellationToken = default)
        => _qualityRuleProvider.Get(name, cancellationToken);

    /// <summary>Gets a quality rule configuration by ID.</summary>
    public Task<IGenericResult<QualityRuleConfiguration>> GetQualityRule(Guid id, CancellationToken cancellationToken = default)
        => _qualityRuleProvider.Get(id, cancellationToken);

    /// <summary>Gets all quality rule configurations.</summary>
    public Task<IGenericResult<IReadOnlyList<QualityRuleConfiguration>>> GetAllQualityRules(CancellationToken cancellationToken = default)
        => _qualityRuleProvider.Get(cancellationToken);

    /// <summary>Gets a DataSet annotation configuration by name.</summary>
    public Task<IGenericResult<DataSetAnnotationConfiguration>> GetAnnotation(string name, CancellationToken cancellationToken = default)
        => _annotationProvider.Get(name, cancellationToken);

    /// <summary>Gets a DataSet annotation configuration by ID.</summary>
    public Task<IGenericResult<DataSetAnnotationConfiguration>> GetAnnotation(Guid id, CancellationToken cancellationToken = default)
        => _annotationProvider.Get(id, cancellationToken);

    /// <summary>Gets all DataSet annotation configurations.</summary>
    public Task<IGenericResult<IReadOnlyList<DataSetAnnotationConfiguration>>> GetAllAnnotations(CancellationToken cancellationToken = default)
        => _annotationProvider.Get(cancellationToken);

    /// <summary>Gets an environment configuration by name.</summary>
    public Task<IGenericResult<EnvironmentConfiguration>> GetEnvironment(string name, CancellationToken cancellationToken = default)
        => _environmentProvider.Get(name, cancellationToken);

    /// <summary>Gets an environment configuration by ID.</summary>
    public Task<IGenericResult<EnvironmentConfiguration>> GetEnvironment(Guid id, CancellationToken cancellationToken = default)
        => _environmentProvider.Get(id, cancellationToken);

    /// <summary>Gets all environment configurations.</summary>
    public Task<IGenericResult<IReadOnlyList<EnvironmentConfiguration>>> GetAllEnvironments(CancellationToken cancellationToken = default)
        => _environmentProvider.Get(cancellationToken);

    /// <summary>Gets a promotion request configuration by name.</summary>
    public Task<IGenericResult<PromotionRequestConfiguration>> GetPromotionRequest(string name, CancellationToken cancellationToken = default)
        => _promotionRequestProvider.Get(name, cancellationToken);

    /// <summary>Gets a promotion request configuration by ID.</summary>
    public Task<IGenericResult<PromotionRequestConfiguration>> GetPromotionRequest(Guid id, CancellationToken cancellationToken = default)
        => _promotionRequestProvider.Get(id, cancellationToken);

    /// <summary>Gets all promotion request configurations.</summary>
    public Task<IGenericResult<IReadOnlyList<PromotionRequestConfiguration>>> GetAllPromotionRequests(CancellationToken cancellationToken = default)
        => _promotionRequestProvider.Get(cancellationToken);

    /// <summary>Gets a glossary term configuration by name.</summary>
    public Task<IGenericResult<GlossaryTermConfiguration>> GetGlossaryTerm(string name, CancellationToken cancellationToken = default)
        => _glossaryTermProvider.Get(name, cancellationToken);

    /// <summary>Gets a glossary term configuration by ID.</summary>
    public Task<IGenericResult<GlossaryTermConfiguration>> GetGlossaryTerm(Guid id, CancellationToken cancellationToken = default)
        => _glossaryTermProvider.Get(id, cancellationToken);

    /// <summary>Gets all glossary term configurations.</summary>
    public Task<IGenericResult<IReadOnlyList<GlossaryTermConfiguration>>> GetAllGlossaryTerms(CancellationToken cancellationToken = default)
        => _glossaryTermProvider.Get(cancellationToken);

    // ============================================================================================
    // Save overloads — endpoint-facing. DTO→config mapping lives here, next to the config type.
    // ============================================================================================

    /// <summary>Saves a quality rule configuration (upsert by Id).</summary>
    public Task<IGenericResult<QualityRuleConfiguration>> SaveQualityRule(QualityRuleConfiguration config, CancellationToken ct = default)
        => _qualityRuleProvider.Save(config, ct);

    /// <summary>Saves a DataSet annotation configuration (upsert by Id).</summary>
    public Task<IGenericResult<DataSetAnnotationConfiguration>> SaveAnnotation(DataSetAnnotationConfiguration config, CancellationToken ct = default)
        => _annotationProvider.Save(config, ct);

    /// <summary>Saves a glossary term configuration (upsert by Id).</summary>
    public Task<IGenericResult<GlossaryTermConfiguration>> SaveGlossaryTerm(GlossaryTermConfiguration config, CancellationToken ct = default)
        => _glossaryTermProvider.Save(config, ct);

    // ============================================================================================
    // Delete overloads — by Guid and by name.
    // ============================================================================================

    /// <summary>Deletes a quality rule by Id.</summary>
    public Task<IGenericResult> DeleteQualityRule(Guid id, CancellationToken ct = default)
        => _qualityRuleProvider.Delete(id, ct);

    /// <summary>Deletes a quality rule by name.</summary>
    public Task<IGenericResult> DeleteQualityRule(string name, CancellationToken ct = default)
        => _qualityRuleProvider.Delete(name, ct);

    /// <summary>Deletes a DataSet annotation by Id.</summary>
    public Task<IGenericResult> DeleteAnnotation(Guid id, CancellationToken ct = default)
        => _annotationProvider.Delete(id, ct);

    /// <summary>Deletes a DataSet annotation by name.</summary>
    public Task<IGenericResult> DeleteAnnotation(string name, CancellationToken ct = default)
        => _annotationProvider.Delete(name, ct);

    /// <summary>Deletes a glossary term by Id.</summary>
    public Task<IGenericResult> DeleteGlossaryTerm(Guid id, CancellationToken ct = default)
        => _glossaryTermProvider.Delete(id, ct);

    /// <summary>Deletes a glossary term by name.</summary>
    public Task<IGenericResult> DeleteGlossaryTerm(string name, CancellationToken ct = default)
        => _glossaryTermProvider.Delete(name, ct);

    // ============================================================================================
    // Mappers — DTO → Configuration. No Guid.NewGuid(); DefaultConfigurationProvider mints UUIDv7.
    // ============================================================================================

    /// <summary>
    /// Maps a DataSetAnnotationPayload to a DataSetAnnotationConfiguration for upsert via the provider.
    /// </summary>
    // Why: Owner → BusinessOwner (the "owner" in the API is the accountable business role).
    // Why: Steward → TechnicalOwner (data stewards are the technical custodians of the data).
    // Why: Classification → DataClassification (field renamed in config for clarity).
    // Why: Tags (IList<string>) → Tags (IList<DataSetAnnotationTagConfiguration>), each string
    //      becomes a TagConfiguration with TagValue set. This preserves round-trip fidelity when
    //      the annotation is read back and the tags are presented as strings by the endpoint.
    public static DataSetAnnotationConfiguration MapAnnotationFromDto(string dataSetName, string? owner, string? steward, string? classification, IEnumerable<string>? tags)
        => new()
        {
            // Why: Id is Guid.Empty so DefaultConfigurationProvider.Save mints a UUIDv7 on insert.
            Name = dataSetName,
            DataSetName = dataSetName,
            BusinessOwner = owner,
            TechnicalOwner = steward,
            DataClassification = classification,
            // Why: Each string tag from the DTO maps to a TagConfiguration with Tag=value and Name=value.
            // TagConfiguration.Tag is the stored value; Name satisfies IGenericConfiguration's display contract.
            Tags = tags?.Select(t => new DataSetAnnotationTagConfiguration { Tag = t, Name = t }).ToList()
                ?? []
        };

    /// <summary>Maps a QualityRuleConfiguration to a QualityRuleDto for the API response.</summary>
    public static QualityRuleConfiguration MapQualityRuleFromRequest(
        string dataSetName, string? fieldName, string ruleType, string severity,
        bool isEnabled, string? description, string? minValue, string? maxValue,
        string? pattern, string? expression, string? name = null)
        => new()
        {
            // Why: Id is Guid.Empty so DefaultConfigurationProvider.Save mints a UUIDv7 on insert.
            // Use the caller-supplied Name when present; otherwise synthesize the conventional
            // "{dataSet}:{ruleType}" identifier (existing behavior, not a new fallback value).
            Name = string.IsNullOrWhiteSpace(name) ? $"{dataSetName}:{ruleType}" : name,
            DataSetName = dataSetName,
            FieldName = fieldName,
            RuleType = ruleType,
            Severity = severity,
            IsEnabled = isEnabled,
            Description = description,
            MinValue = minValue,
            MaxValue = maxValue,
            Pattern = pattern,
            Expression = expression
        };
}
