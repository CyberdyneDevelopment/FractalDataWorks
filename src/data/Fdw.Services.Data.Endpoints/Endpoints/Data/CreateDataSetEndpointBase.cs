using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions.Results;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.DataSets.Results;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Data.Endpoints.Logging;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Generic base endpoint for creating a new data set configuration.
/// Delegates all reads and writes to IDataSetConfigurationProvider.
/// </summary>
public abstract class CreateDataSetEndpointBase : CrudCreateEndpointBase<CreateDataSetRequest, DataSetDetailResponse>
{
    private readonly IDataSetConfigurationProvider _dataSetProvider;
    private readonly ICategoryConfigurationProvider _categories;
    private readonly IAuthenticationContextAccessor _authContext;
    private readonly IDataStoreConfigurationProvider _dataStores;

    /// <summary>Initializes a new instance of the <see cref="CreateDataSetEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dataSetProvider">Finds and saves the data set.</param>
    /// <param name="categories">Resolves the category the request names.</param>
    /// <param name="authContext">Identifies the caller, who owns the new data set (FDW-4346).</param>
    /// <param name="dataStores">Reads the data stores the sources name, from which the data set's kind is worked out (FDW-6806).</param>
    protected CreateDataSetEndpointBase(
        ILogger<CreateDataSetEndpointBase> logger, IDataSetConfigurationProvider dataSetProvider, ICategoryConfigurationProvider categories,
        IAuthenticationContextAccessor authContext, IDataStoreConfigurationProvider dataStores) : base(logger)
    {
        _dataSetProvider = dataSetProvider;
        _categories = categories;
        _authContext = authContext;
        _dataStores = dataStores;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "datasets";

    /// <summary>Returns the data set name from the create request.</summary>
    protected override string GetResourceName(CreateDataSetRequest request) => request.Name;

    /// <summary>Checks whether a data set with the requested name already exists.</summary>
    protected override Task<IGenericResult<bool>> CheckExists(CreateDataSetRequest request, CancellationToken ct)
    {
        return CheckDataSetExists(request.Name, ct);
    }

    /// <summary>Creates the data set configuration and persists it.</summary>
    protected override Task<IGenericResult<DataSetDetailResponse>> Create(CreateDataSetRequest request, CancellationToken ct)
    {
        return CreateDataSet(request, ct);
    }

    /// <summary>
    /// Checks if a data set already exists by querying the provider.
    /// Override for custom behavior.
    /// </summary>
    protected virtual async Task<IGenericResult<bool>> CheckDataSetExists(string name, CancellationToken ct)
    {
        var result = await _dataSetProvider.Get(name, ct).ConfigureAwait(false);
        if (result.IsFailure) return result.ToNewResult<bool>();

        var exists = result.Value is not null;
        if (exists) DataSetEndpointLog.DataSetAlreadyExists(Logger, name);
        return GenericResult<bool>.Success(exists);
    }

    /// <summary>
    /// Creates and saves a new data set, returning as detail DTO.
    /// Override for custom behavior.
    /// </summary>
    protected virtual async Task<IGenericResult<DataSetDetailResponse>> CreateDataSet(CreateDataSetRequest request, CancellationToken ct)
    {
        DataSetEndpointLog.CreatingDataSet(Logger, request.Name);

        // The kind is worked out from the sources, never stated (FDW-6806).
        var kind = await DataSetKindDerivation.Derive(
            request.Name, [.. request.Sources.Select(source => (source.SourceName, (string?)source.DataStoreName, source.SourceKind))], _dataStores, Logger, ct)
            .ConfigureAwait(false);
        if (kind.IsFailure) return kind.ToNewResult<DataSetDetailResponse>();

        var federationValidation = DataSetQueryHelper.ValidateFederationStrategy(
            kind.Value!, request.FederationStrategy, request.Name, Logger);
        if (federationValidation.IsFailure) return federationValidation.ToNewResult<DataSetDetailResponse>();

        var partsValidation = DataSetQueryHelper.ValidateKindParts(
            kind.Value!, request.Joins.Count > 0, request.Aggregates.Count > 0, request.Name, Logger);
        if (partsValidation.IsFailure) return partsValidation.ToNewResult<DataSetDetailResponse>();

        var aggregatesValidation = DataSetQueryHelper.ValidateAggregates(request.Aggregates, request.Name, Logger);
        if (aggregatesValidation.IsFailure) return aggregatesValidation.ToNewResult<DataSetDetailResponse>();

        var mappingsValidation = DataSetQueryHelper.ValidateFieldMappings(request.Sources, request.Name, Logger);
        if (mappingsValidation.IsFailure) return mappingsValidation.ToNewResult<DataSetDetailResponse>();

        // The data set's classification and each field's, before anything is written.
        var classified = DataClassificationValidator.Validate(request.Fields.Select(f => f.ClassificationId).Prepend(request.ClassificationId), Logger);
        if (classified.IsFailure) return classified.ToNewResult<DataSetDetailResponse>();

        // Each field's datum type and data type.
        var typed = DataSetQueryHelper.ValidateFieldTypes(request.Fields, Logger);
        if (typed.IsFailure) return typed.ToNewResult<DataSetDetailResponse>();

        // The category the request names, before anything is written.
        var category = await DataSetCategoryLinks.Requested(_categories, request.Category, Logger, ct).ConfigureAwait(false);
        if (category.IsFailure) return category.ToNewResult<DataSetDetailResponse>();

        // The caller owns the data set they create; a caller that cannot be named is refused, never stored as no owner (FDW-4346).
        if (_authContext.Current is not { } caller)
        {
            return GenericResult<DataSetDetailResponse>.Failure(
                DataSetsResultCodes.ByName("OwnerUnresolved"), Logger,
                ResultDetails.Create("name", request.Name, "reason", "no authentication context"));
        }

        if (!Guid.TryParse(caller.UserId, out var ownerUserId))
        {
            return GenericResult<DataSetDetailResponse>.Failure(
                DataSetsResultCodes.ByName("OwnerUnresolved"), Logger,
                ResultDetails.Create("name", request.Name, "reason", "UserId is not a Guid"));
        }

        // A value the caller chooses is refused when left out, never assumed (FDW-3931).
        var built = DataSetConfigurationFactory.Create(request, kind.Value!, category.Value.Id, ownerUserId, new DataSetRequestValues(request.Name, Logger), Logger);
        if (built.IsFailure) return built.ToNewResult<DataSetDetailResponse>();

        var config = built.Value!;
        var saveResult = await _dataSetProvider.Save(config, "DataSet", config.Implementation, config.Name, ct).ConfigureAwait(false);
        if (saveResult.IsFailure)
        {
            DataSetEndpointLog.DataSetCreateFailed(Logger, request.Name, "Save failed");
            return saveResult.ToNewResult<DataSetDetailResponse>();
        }

        DataSetEndpointLog.DataSetCreated(Logger, config.Name);
        return DataSetCategoryLinks.Describe(DataSetQueryHelper.MapToDetail(config), category.Value.Tree, config.CategoryId, Logger);
    }

    /// <summary>Sends a 201 Created response with the data set detail.</summary>
    protected override Task SendCreatedResponse(DataSetDetailResponse detail, CancellationToken ct)
    {
        return Send.ResponseAsync(detail, 201, ct);
    }
}
