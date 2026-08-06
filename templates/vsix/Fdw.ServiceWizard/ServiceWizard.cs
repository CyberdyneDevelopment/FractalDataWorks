using System;
using System.Collections.Generic;
using System.Windows.Forms;
using EnvDTE;
using Microsoft.VisualStudio.TemplateWizard;

namespace Fdw.ServiceWizard;

/// <summary>
/// Template wizard for creating Fdw service domains and implementations.
/// Presents a menu-driven UI for selecting what to generate.
/// </summary>
public class ServiceWizard : IWizard
{
    private ServiceWizardForm? _wizardForm;
    private bool _cancelled;

    public void RunStarted(
        object automationObject,
        Dictionary<string, string> replacementsDictionary,
        WizardRunKind runKind,
        object[] customParams)
    {
        try
        {
            _wizardForm = new ServiceWizardForm();
            var result = _wizardForm.ShowDialog();

            if (result != DialogResult.OK)
            {
                _cancelled = true;
                return;
            }

            // Set replacement parameters from wizard
            replacementsDictionary["$serviceName$"] = _wizardForm.ServiceName;
            replacementsDictionary["$serviceNameLower$"] = _wizardForm.ServiceName.ToLowerInvariant();
            replacementsDictionary["$implName$"] = _wizardForm.ImplName ?? string.Empty;
            replacementsDictionary["$implNameLower$"] = _wizardForm.ImplName?.ToLowerInvariant() ?? string.Empty;
            replacementsDictionary["$namespace$"] = _wizardForm.Namespace;
            replacementsDictionary["$createDomain$"] = _wizardForm.CreateDomain.ToString();
            replacementsDictionary["$createImpl$"] = _wizardForm.CreateImplementation.ToString();
            replacementsDictionary["$includeProvider$"] = _wizardForm.IncludeProvider.ToString();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error starting wizard: {ex.Message}",
                "Fdw Service Wizard",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            _cancelled = true;
        }
    }

    public void ProjectFinishedGenerating(Project project)
    {
        // Optional: Post-generation actions
    }

    public void ProjectItemFinishedGenerating(ProjectItem projectItem)
    {
        // Optional: Per-item post-generation actions
    }

    public bool ShouldAddProjectItem(string filePath)
    {
        if (_cancelled)
            return false;

        if (_wizardForm == null)
            return true;

        // Filter files based on wizard selections
        var fileName = System.IO.Path.GetFileName(filePath);

        // Domain-only files
        if (!_wizardForm.CreateDomain)
        {
            if (fileName.Contains("Abstractions") ||
                fileName.Contains("TypeBase") ||
                fileName.Contains("Types.cs") ||
                fileName.Contains("DefaultProvider"))
            {
                return false;
            }
        }

        // Implementation-only files
        if (!_wizardForm.CreateImplementation)
        {
            if (fileName.Contains(_wizardForm.ImplName ?? "Impl"))
            {
                return false;
            }
        }

        // Provider is optional for domain
        if (!_wizardForm.IncludeProvider && fileName.Contains("DefaultProvider"))
        {
            return false;
        }

        return true;
    }

    public void BeforeOpeningFile(ProjectItem projectItem)
    {
        // Optional: Actions before opening generated files
    }

    public void RunFinished()
    {
        // Optional: Final cleanup
    }
}
