using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fdw.CodeBuilder.Abstractions;

namespace Fdw.CodeBuilder.CSharp.Builders;

/// <summary>
/// Builder for generating C# class definitions.
/// </summary>
public sealed class ClassBuilder : CodeBuilderBase, IClassBuilder
{
    private static readonly string[] NewLineSeparators = ["\r\n", "\n"];
    private string? _namespace;
    private readonly List<string> _usings = [];
    private string _className = "MyClass";
    private string _accessModifier = "public";
    private bool _isStatic;
    private bool _isAbstract;
    private bool _isSealed;
    private bool _isPartial;
    private string? _baseClass;
    private readonly List<string> _interfaces = [];
    private readonly List<string> _genericParameters = [];
    private readonly Dictionary<string, List<string>> _genericConstraints = new(StringComparer.Ordinal);
    private readonly List<string> _attributes = [];
    private readonly List<IFieldBuilder> _fields = [];
    private readonly List<IPropertyBuilder> _properties = [];
    private readonly List<IMethodBuilder> _methods = [];
    private readonly List<IConstructorBuilder> _constructors = [];
    private readonly List<IClassBuilder> _nestedClasses = [];
    private string? _xmlDocSummary;

    /// <inheritdoc/>
    public IClassBuilder WithNamespace(string namespaceName)
    {
        _namespace = namespaceName;
        return this;
    }

    /// <inheritdoc/>
    public IClassBuilder WithUsings(params string[] usings)
    {
        _usings.AddRange(usings);
        return this;
    }

    /// <inheritdoc/>
    public IClassBuilder WithName(string className)
    {
        _className = className;
        return this;
    }

    /// <inheritdoc/>
    public IClassBuilder WithAccessModifier(string accessModifier)
    {
        _accessModifier = accessModifier;
        return this;
    }

    /// <inheritdoc/>
    public IClassBuilder AsStatic()
    {
        _isStatic = true;
        return this;
    }

    /// <inheritdoc/>
    public IClassBuilder AsAbstract()
    {
        _isAbstract = true;
        _isSealed = false;
        return this;
    }

    /// <inheritdoc/>
    public IClassBuilder AsSealed()
    {
        _isSealed = true;
        _isAbstract = false;
        return this;
    }

    /// <inheritdoc/>
    public IClassBuilder AsPartial()
    {
        _isPartial = true;
        return this;
    }

    /// <inheritdoc/>
    public IClassBuilder WithBaseClass(string baseClass)
    {
        _baseClass = baseClass;
        return this;
    }

    /// <inheritdoc/>
    public IClassBuilder WithInterfaces(params string[] interfaces)
    {
        _interfaces.AddRange(interfaces);
        return this;
    }

    /// <inheritdoc/>
    public IClassBuilder WithGenericParameters(params string[] typeParameters)
    {
        _genericParameters.AddRange(typeParameters);
        return this;
    }

    /// <inheritdoc/>
    public IClassBuilder WithGenericConstraint(string typeParameter, params string[] constraints)
    {
        if (!_genericConstraints.ContainsKey(typeParameter))
            _genericConstraints[typeParameter] = [];
        _genericConstraints[typeParameter].AddRange(constraints);
        return this;
    }

    /// <inheritdoc/>
    public IClassBuilder WithAttribute(string attribute)
    {
        _attributes.Add(attribute);
        return this;
    }

    /// <inheritdoc/>
    public IClassBuilder WithField(IFieldBuilder fieldBuilder)
    {
        _fields.Add(fieldBuilder);
        return this;
    }

    /// <inheritdoc/>
    public IClassBuilder WithProperty(IPropertyBuilder propertyBuilder)
    {
        _properties.Add(propertyBuilder);
        return this;
    }

    /// <inheritdoc/>
    public IClassBuilder WithMethod(IMethodBuilder methodBuilder)
    {
        _methods.Add(methodBuilder);
        return this;
    }

    /// <inheritdoc/>
    public IClassBuilder WithConstructor(IConstructorBuilder constructorBuilder)
    {
        _constructors.Add(constructorBuilder);
        return this;
    }

    /// <inheritdoc/>
    public IClassBuilder WithNestedClass(IClassBuilder nestedClassBuilder)
    {
        _nestedClasses.Add(nestedClassBuilder);
        return this;
    }

    /// <inheritdoc/>
    public IClassBuilder WithXmlDoc(string summary)
    {
        _xmlDocSummary = summary;
        return this;
    }

    /// <inheritdoc/>
    public override string Build()
    {
        Clear();

        BuildUsingDirectives();
        BuildNamespace();
        BuildXmlDocumentation();
        BuildAttributes();
        BuildClassDeclaration();
        BuildGenericConstraints();

        AppendLine("{");
        Indent();

        BuildClassMembers();

        Outdent();
        AppendLine("}");

        return Builder.ToString();
    }

    private void BuildUsingDirectives()
    {
        if (_usings.Count > 0)
        {
            var uniqueUsings = new HashSet<string>(_usings, StringComparer.Ordinal);
            foreach (var usingDirective in uniqueUsings.OrderBy(u => u, StringComparer.Ordinal))
            {
                AppendLine($"using {usingDirective};");
            }
            AppendLine("");
        }
    }

    private void BuildNamespace()
    {
        if (!string.IsNullOrEmpty(_namespace))
        {
            AppendLine($"namespace {_namespace};");
            AppendLine("");
        }
    }

    private void BuildXmlDocumentation()
    {
        if (!string.IsNullOrEmpty(_xmlDocSummary))
        {
            AppendLine("/// <summary>");
            foreach (var line in _xmlDocSummary!.Split(NewLineSeparators, StringSplitOptions.RemoveEmptyEntries))
            {
                AppendLine($"/// {EscapeXmlEntities(line.Trim())}");
            }
            AppendLine("/// </summary>");
        }
    }


    private void BuildAttributes()
    {
        foreach (var attribute in _attributes)
        {
            AppendLine($"[{attribute}]");
        }
    }

    private void BuildClassDeclaration()
    {
        var declaration = new StringBuilder();
        declaration.Append(_accessModifier);

        if (_isStatic) declaration.Append(" static");
        if (_isAbstract) declaration.Append(" abstract");
        if (_isSealed) declaration.Append(" sealed");
        if (_isPartial) declaration.Append(" partial");

        declaration.Append(" class ");
        declaration.Append(_className);

        if (_genericParameters.Count > 0)
        {
            declaration.Append($"<{string.Join(", ", _genericParameters)}>");
        }

        BuildInheritanceClause(declaration);
        AppendLine(declaration.ToString());
    }

    private void BuildInheritanceClause(StringBuilder declaration)
    {
        var inheritance = new List<string>();
        if (!string.IsNullOrEmpty(_baseClass))
            inheritance.Add(_baseClass!);
        inheritance.AddRange(_interfaces);

        if (inheritance.Count > 0)
        {
            declaration.Append($" : {string.Join(", ", inheritance)}");
        }
    }

    private void BuildGenericConstraints()
    {
        foreach (var constraint in _genericConstraints)
        {
            Indent();
            AppendLine($"where {constraint.Key} : {string.Join(", ", constraint.Value)}");
            Outdent();
        }
    }

    private void BuildClassMembers()
    {
        BuildFields();
        BuildConstructors();
        BuildProperties();
        BuildMethods();
        BuildNestedClasses();
    }

    private void BuildFields()
    {
        foreach (var field in _fields)
        {
            var fieldCode = field.Build();
            foreach (var line in fieldCode.Split(NewLineSeparators, StringSplitOptions.RemoveEmptyEntries))
            {
                AppendLine(line);
            }
        }

        if (_fields.Count > 0 && (_constructors.Count > 0 || _properties.Count > 0 || _methods.Count > 0))
            AppendLine("");
    }

    private void BuildConstructors()
    {
        foreach (var constructor in _constructors)
        {
            var constructorCode = constructor.Build();
            foreach (var line in constructorCode.Split(NewLineSeparators, StringSplitOptions.RemoveEmptyEntries))
            {
                AppendLine(line);
            }
            AppendLine("");
        }
    }

    private void BuildProperties()
    {
        foreach (var property in _properties)
        {
            var propertyCode = property.Build();
            foreach (var line in propertyCode.Split(NewLineSeparators, StringSplitOptions.RemoveEmptyEntries))
            {
                AppendLine(line);
            }
            AppendLine("");
        }
    }

    private void BuildMethods()
    {
        foreach (var method in _methods)
        {
            var methodCode = method.Build();
            foreach (var line in methodCode.Split(NewLineSeparators, StringSplitOptions.RemoveEmptyEntries))
            {
                AppendLine(line);
            }
            AppendLine("");
        }
    }

    private void BuildNestedClasses()
    {
        foreach (var nestedClass in _nestedClasses)
        {
            var nestedCode = nestedClass.Build();
            foreach (var line in nestedCode.Split(NewLineSeparators, StringSplitOptions.RemoveEmptyEntries))
            {
                AppendLine(line);
            }
            AppendLine("");
        }
    }
}
