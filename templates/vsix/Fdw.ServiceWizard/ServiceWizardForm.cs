using System;
using System.Drawing;
using System.Windows.Forms;

namespace Fdw.ServiceWizard;

/// <summary>
/// Menu-driven wizard form for configuring service generation.
/// </summary>
public class ServiceWizardForm : Form
{
    private TextBox _txtServiceName = null!;
    private TextBox _txtImplName = null!;
    private TextBox _txtNamespace = null!;
    private CheckBox _chkCreateDomain = null!;
    private CheckBox _chkCreateImpl = null!;
    private CheckBox _chkIncludeProvider = null!;
    private Button _btnOk = null!;
    private Button _btnCancel = null!;
    private GroupBox _grpDomain = null!;
    private GroupBox _grpImpl = null!;

    public string ServiceName => _txtServiceName.Text.Trim();
    public string? ImplName => string.IsNullOrWhiteSpace(_txtImplName.Text) ? null : _txtImplName.Text.Trim();
    public string Namespace => _txtNamespace.Text.Trim();
    public bool CreateDomain => _chkCreateDomain.Checked;
    public bool CreateImplementation => _chkCreateImpl.Checked;
    public bool IncludeProvider => _chkIncludeProvider.Checked;

    public ServiceWizardForm()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text = "Fdw Service Wizard";
        Size = new Size(500, 450);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var lblTitle = new Label
        {
            Text = "Create Fdw Service",
            Font = new Font(Font.FontFamily, 14, FontStyle.Bold),
            Location = new Point(20, 15),
            AutoSize = true
        };

        var lblDescription = new Label
        {
            Text = "Choose what to generate: domain contracts, implementation, or both.",
            Location = new Point(20, 45),
            Size = new Size(440, 20)
        };

        // Service Name
        var lblServiceName = new Label
        {
            Text = "Service Name:",
            Location = new Point(20, 80),
            AutoSize = true
        };

        _txtServiceName = new TextBox
        {
            Location = new Point(140, 77),
            Size = new Size(200, 23),
            PlaceholderText = "e.g., Notification, DataStore"
        };

        // Namespace
        var lblNamespace = new Label
        {
            Text = "Root Namespace:",
            Location = new Point(20, 110),
            AutoSize = true
        };

        _txtNamespace = new TextBox
        {
            Location = new Point(140, 107),
            Size = new Size(300, 23),
            Text = "Fdw.Services"
        };

        // Domain Group
        _grpDomain = new GroupBox
        {
            Text = "Domain (Abstractions + Base)",
            Location = new Point(20, 145),
            Size = new Size(440, 90)
        };

        _chkCreateDomain = new CheckBox
        {
            Text = "Create domain contracts",
            Location = new Point(15, 25),
            AutoSize = true,
            Checked = true
        };

        _chkIncludeProvider = new CheckBox
        {
            Text = "Include default provider",
            Location = new Point(35, 50),
            AutoSize = true,
            Checked = true
        };

        _grpDomain.Controls.Add(_chkCreateDomain);
        _grpDomain.Controls.Add(_chkIncludeProvider);

        // Implementation Group
        _grpImpl = new GroupBox
        {
            Text = "Implementation",
            Location = new Point(20, 245),
            Size = new Size(440, 90)
        };

        _chkCreateImpl = new CheckBox
        {
            Text = "Create implementation project",
            Location = new Point(15, 25),
            AutoSize = true,
            Checked = true
        };

        var lblImplName = new Label
        {
            Text = "Implementation Name:",
            Location = new Point(35, 55),
            AutoSize = true
        };

        _txtImplName = new TextBox
        {
            Location = new Point(175, 52),
            Size = new Size(150, 23),
            PlaceholderText = "e.g., Email, MsSql"
        };

        _grpImpl.Controls.Add(_chkCreateImpl);
        _grpImpl.Controls.Add(lblImplName);
        _grpImpl.Controls.Add(_txtImplName);

        // Buttons
        _btnOk = new Button
        {
            Text = "Create",
            DialogResult = DialogResult.OK,
            Location = new Point(280, 360),
            Size = new Size(80, 30)
        };

        _btnCancel = new Button
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            Location = new Point(370, 360),
            Size = new Size(80, 30)
        };

        // Event handlers
        _chkCreateDomain.CheckedChanged += (s, e) =>
        {
            _chkIncludeProvider.Enabled = _chkCreateDomain.Checked;
            ValidateForm();
        };

        _chkCreateImpl.CheckedChanged += (s, e) =>
        {
            _txtImplName.Enabled = _chkCreateImpl.Checked;
            ValidateForm();
        };

        _txtServiceName.TextChanged += (s, e) => ValidateForm();
        _txtImplName.TextChanged += (s, e) => ValidateForm();

        AcceptButton = _btnOk;
        CancelButton = _btnCancel;

        Controls.AddRange(new Control[]
        {
            lblTitle, lblDescription,
            lblServiceName, _txtServiceName,
            lblNamespace, _txtNamespace,
            _grpDomain, _grpImpl,
            _btnOk, _btnCancel
        });

        ValidateForm();
    }

    private void ValidateForm()
    {
        var valid = !string.IsNullOrWhiteSpace(_txtServiceName.Text);

        if (_chkCreateImpl.Checked && string.IsNullOrWhiteSpace(_txtImplName.Text))
        {
            valid = false;
        }

        if (!_chkCreateDomain.Checked && !_chkCreateImpl.Checked)
        {
            valid = false;
        }

        _btnOk.Enabled = valid;
    }
}
