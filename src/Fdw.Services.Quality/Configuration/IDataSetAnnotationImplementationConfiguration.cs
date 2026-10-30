using Fdw.Configuration;
using Fdw.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Quality.Configuration;

/// <summary>The contract every DataSetAnnotation implementation carries.</summary>
public interface IDataSetAnnotationImplementationConfiguration : IImplementationConfiguration
{

    /// <summary>Gets or sets the DataSetAnnotation DataSetName.</summary>
    string DataSetName { get; set; }

    /// <summary>Gets or sets the DataSetAnnotation Description.</summary>
    string? Description { get; set; }

    /// <summary>Gets or sets the DataSetAnnotation BusinessOwner.</summary>
    string? BusinessOwner { get; set; }

    /// <summary>Gets or sets the DataSetAnnotation TechnicalOwner.</summary>
    string? TechnicalOwner { get; set; }

    /// <summary>Gets or sets the DataSetAnnotation UpdateFrequency.</summary>
    string? UpdateFrequency { get; set; }

    /// <summary>Gets or sets the DataSetAnnotation DataClassification.</summary>
    string? DataClassification { get; set; }

    /// <summary>Gets or sets whether the annotation has been resolved.</summary>
    bool IsResolved { get; set; }

    /// <summary>Gets or sets when the annotation was resolved; empty while it is not resolved.</summary>
    DateTimeOffset? ResolvedDate { get; set; }

    /// <summary>Gets or sets the user id of the person who resolved the annotation; empty while it is not resolved.</summary>
    Guid? ResolvedBy { get; set; }

    /// <summary>Gets or sets the DataSetAnnotation Tags.</summary>
    IList<DataSetAnnotationTagConfiguration> Tags { get; set; }

    /// <summary>Gets or sets the DataSetAnnotation FieldDescriptions.</summary>
    IList<DataSetAnnotationFieldDescriptionConfiguration> FieldDescriptions { get; set; }

    /// <summary>Gets or sets the DataSetAnnotation FieldBusinessNames.</summary>
    IList<DataSetAnnotationFieldBusinessNameConfiguration> FieldBusinessNames { get; set; }

}
