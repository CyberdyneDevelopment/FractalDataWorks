using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Shouldly;
using Fdw.CodeBuilder.Analysis.CSharp.Helpers;

namespace Fdw.CodeBuilder.Analysis.CSharp.Expectations.ClassExpectationsDelegates;

/// <summary>
/// Delegate responsible for handling member-related expectations for a class.
/// </summary>
[ExcludeFromCodeCoverage]
//<ExcludedFromCoverage>Internal implementation detail for delegation pattern - covered by parent class tests</ExcludedFromCoverage>
internal sealed class ClassMemberExpectationsDelegate
{
    private readonly ClassDeclarationSyntax _classDeclaration;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClassMemberExpectationsDelegate"/> class.
    /// </summary>
    /// <param name="classDeclaration">The class declaration to verify.</param>
    public ClassMemberExpectationsDelegate(ClassDeclarationSyntax classDeclaration)
    {
        _classDeclaration = classDeclaration ?? throw new ArgumentNullException(nameof(classDeclaration));
    }

    /// <summary>
    /// Expects the class to have a method with the specified name.
    /// </summary>
    /// <param name="methodName">The name of the method to find.</param>
    public void HasMethod(string methodName)
    {
        _ = SyntaxNodeHelpers.FindMethod(_classDeclaration, methodName);
    }

    /// <summary>
    /// Expects the class to have a method with the specified name and adds more expectations for that method.
    /// </summary>
    /// <param name="methodName">The name of the method to find.</param>
    /// <param name="methodExpectations">A callback to add expectations for the found method.</param>
    public void HasMethod(string methodName, Action<MethodExpectations> methodExpectations)
    {
        var methodDecl = SyntaxNodeHelpers.FindMethod(_classDeclaration, methodName);
        var methodExp = new MethodExpectations(methodDecl);
        methodExpectations(methodExp);
    }

    /// <summary>
    /// Expects the class to have a property with the specified name.
    /// </summary>
    /// <param name="propertyName">The name of the property to find.</param>
    public void HasProperty(string propertyName)
    {
        _ = SyntaxNodeHelpers.FindProperty(_classDeclaration, propertyName);
    }

    /// <summary>
    /// Expects the class to have a property with the specified name and adds more expectations for that property.
    /// </summary>
    /// <param name="propertyName">The name of the property to find.</param>
    /// <param name="propertyExpectations">A callback to add expectations for the found property.</param>
    public void HasProperty(string propertyName, Action<PropertyExpectations> propertyExpectations)
    {
        var propertyDecl = SyntaxNodeHelpers.FindProperty(_classDeclaration, propertyName);
        var propertyExp = new PropertyExpectations(propertyDecl);
        propertyExpectations(propertyExp);
    }

    /// <summary>
    /// Expects the class to have a field with the specified name.
    /// </summary>
    /// <param name="fieldName">The name of the field to find.</param>
    public void HasField(string fieldName)
    {
        _ = SyntaxNodeHelpers.FindField(_classDeclaration, fieldName);
    }

    /// <summary>
    /// Expects the class to have a field with the specified name and adds more expectations for that field.
    /// </summary>
    /// <param name="fieldName">The name of the field to find.</param>
    /// <param name="fieldExpectations">A callback to add expectations for the found field.</param>
    public void HasField(string fieldName, Action<FieldExpectations> fieldExpectations)
    {
        var fieldDecl = SyntaxNodeHelpers.FindField(_classDeclaration, fieldName);
        var fieldExp = new FieldExpectations(fieldDecl);
        fieldExpectations(fieldExp);
    }

    /// <summary>
    /// Expects the class to have exactly the specified number of constructors.
    /// </summary>
    /// <param name="count">The expected number of constructors.</param>
    public void HasConstructorCount(int count)
    {
        var constructors = _classDeclaration.Members
            .OfType<ConstructorDeclarationSyntax>()
            .Where(c => !c.Modifiers.Any(m => string.Equals(m.ValueText, "static", StringComparison.Ordinal)))
            .ToList();

        constructors.Count.ShouldBe(count, $"Expected class '{_classDeclaration.Identifier}' to have {count} constructor(s) but found {constructors.Count}");
    }

    /// <summary>
    /// Gets a collection of property names defined in this class.
    /// </summary>
    public IEnumerable<string> Properties =>
        _classDeclaration.Members
            .OfType<PropertyDeclarationSyntax>()
            .Select(p => p.Identifier.ValueText);

    /// <summary>
    /// Gets a collection of method names defined in this class.
    /// </summary>
    public IEnumerable<string> Methods =>
        _classDeclaration.Members
            .OfType<MethodDeclarationSyntax>()
            .Select(m => m.Identifier.ValueText);
}
