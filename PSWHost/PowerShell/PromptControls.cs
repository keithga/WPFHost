using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// PSWHost - Copyright (KeithGa@KeithGa.com) all rights reserved.
// Apache License 2.0

namespace PSWHost
{
    using System.Windows;
    using System.Management.Automation;
    using System.Management.Automation.Host;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.ComponentModel;
    using System.Windows.Input;
    using System.Security;

    #region Parent Class Prompt Control


    /// <summary>
    /// $Host.UI.Prompt can accept many differnet classes/types of prompts.
    /// Each type needs to be visualized and processed by the UI.
    /// This is the parent class used to display types in a WPF window.
    /// </summary>
    class PromptControl
    {
        protected FieldDescription _fieldDescription = null;
        protected object _value = null;
        protected Label ErrorLabel = null;
        protected StackPanel MainContainer = null;

        public PromptControl()
        {
            this.MainContainer = new StackPanel() { Orientation = Orientation.Horizontal };
            this.ErrorLabel = new Label()
            {
                Padding = new Thickness(0),
                Visibility = Visibility.Collapsed,
                Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("Red")),
                Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("Yellow"))
            };
        }

        public FieldDescription FieldDescription
        {
            get { return _fieldDescription; }
        }

        public event RoutedEventHandler ControlChanged = null;

        public virtual void OnControlChanged(object sender, RoutedEventArgs e)
        {
            if (ControlChanged != null)
                ControlChanged(sender, e);
        }

        public virtual PSObject Value
        {
            get
            {
                if (isValueOK)
                    return new PSObject(_value);
                return null;
            }
        }

        public virtual bool isValueOK
        { get { return false; } }

        public FrameworkElement GetControl
        {
            get { return MainContainer; }
        }

        public virtual void SetFocus()
        { }

        protected bool EnableError(string Error )
        {
            this.ErrorLabel.Content = Error;
            this.ErrorLabel.Visibility = Visibility.Visible;
            return false;
        }

        protected void DisableError()
        {
            this.ErrorLabel.Visibility = Visibility.Collapsed;

        }
    }

    #endregion

    #region PromptTextBox and PromptPasswordBox

    /// <summary>
    /// Display a text box for a string or other simple type.
    /// </summary>
    class PromptTextBox : PromptControl
    {
        private Label NameLabel = null;
        protected TextBox MainEdit = null;

        public PromptTextBox()
        {
        }

        public PromptTextBox (FieldDescription fieldDescription )
        {
            this.SetFieldDescription(fieldDescription);
        }

        protected void SetFieldDescription(FieldDescription fieldDescription)
        {
            _fieldDescription = fieldDescription;

            this.NameLabel = new Label() { Content = FieldDescription.Name + ": " };
            this.MainEdit = new TextBox() { MinWidth = 100, Height = 20 };

            this.MainContainer.Children.Add(this.NameLabel);
            this.MainContainer.Children.Add(this.MainEdit);
            this.MainContainer.Children.Add(this.ErrorLabel);

            if ((_fieldDescription.DefaultValue != null) && (!string.IsNullOrEmpty(_fieldDescription.DefaultValue.BaseObject.ToString())))
                this.MainEdit.Text = _fieldDescription.DefaultValue.BaseObject.ToString();

            if ((_fieldDescription.HelpMessage != null) && (!string.IsNullOrEmpty(_fieldDescription.HelpMessage)))
                this.MainEdit.ToolTip = _fieldDescription.HelpMessage;

            this.MainEdit.TextChanged += OnControlChanged;
            this.MainEdit.SelectAll();
        }

        public override bool isValueOK
        {
            get
            {
                if (_fieldDescription.IsMandatory && string.IsNullOrEmpty(this.MainEdit.Text))
                    return this.EnableError(PowerShell.Strings.Missing);

                try
                {
                    _value = TypeDescriptor.GetConverter(Type.GetType(_fieldDescription.ParameterTypeFullName)).ConvertFromInvariantString(MainEdit.Text).ToString();
                }
                catch (System.Exception ex)
                {
                    return this.EnableError(ex.Message);
                }
                this.DisableError();
                return true;
            }
        }


        public override void SetFocus()
        {
            this.MainEdit.Focus();
            Keyboard.Focus(this.MainEdit);
        }

    }

    /// <summary>
    /// Display a multiline text box.
    /// </summary>
    class PromptMultiLineTextBox : PromptTextBox
    {
        public PromptMultiLineTextBox(FieldDescription fieldDescription)
        {
            SetFieldDescription(fieldDescription);
            this.MainEdit.AcceptsReturn = true;
            this.MainEdit.TextWrapping = TextWrapping.Wrap;
            this.MainEdit.Height = Double.NaN;
        }

        public override bool isValueOK
        {
            get
            {

                if (_fieldDescription.IsMandatory && string.IsNullOrEmpty(this.MainEdit.Text))
                    return this.EnableError(PowerShell.Strings.Missing);

                string[] stringSeparators = new string[] { "\r\n" };
                _value = MainEdit.Text.Split(stringSeparators, StringSplitOptions.None);

                this.DisableError();
                return true;
            }
        }

    }

    /// <summary>
    /// Display a PasswordBox that returns a SecureString
    /// </summary>
    class PromptPasswordBox : PromptControl
    {
        private Label NameLabel = null;
        private PasswordBox MainEdit = null;

        public PromptPasswordBox(FieldDescription fieldDescription)
        {
            _fieldDescription = fieldDescription;

            this.MainContainer = new StackPanel() { Orientation = Orientation.Horizontal };
            this.NameLabel = new Label() { Content = FieldDescription.Name + ": " };
            this.MainEdit = new PasswordBox() { MinWidth = 100, Height = 20 };

            this.MainContainer.Children.Add(this.NameLabel);
            this.MainContainer.Children.Add(this.MainEdit);
            this.MainContainer.Children.Add(this.ErrorLabel);

            if ((_fieldDescription.DefaultValue != null) && (!string.IsNullOrEmpty(_fieldDescription.DefaultValue.BaseObject.ToString())))
                this.MainEdit.Password = _fieldDescription.DefaultValue.BaseObject.ToString();

            if ((_fieldDescription.HelpMessage != null) && (!string.IsNullOrEmpty(_fieldDescription.HelpMessage)))
                this.MainEdit.ToolTip = _fieldDescription.HelpMessage;

            this.MainEdit.PasswordChanged += OnControlChanged;
            this.MainEdit.SelectAll();

        }

        public override bool isValueOK
        {
            get
            {
                if (_fieldDescription.IsMandatory && string.IsNullOrEmpty(this.MainEdit.Password))
                    return this.EnableError(PowerShell.Strings.Missing);

                _value = this.MainEdit.SecurePassword;
                this.DisableError();
                return true;
            }
        }
        
        public override void SetFocus()
        {
            this.MainEdit.Focus();
            Keyboard.Focus(this.MainEdit);
        }

    }

    #endregion

    #region Prompt Credential Box

    /// <summary>
    /// Handler for a PSCredential type.
    /// Rather than manually draw both the TextBox and the PasswordBox
    /// we should wrap arround a PromptTextBox and a PromptPasswordBox
    /// </summary>
    class PromptCredentialBox : PromptControl
    {
        private PromptTextBox MainUserName = null;
        private PromptPasswordBox MainPassword = null;

        public PromptCredentialBox(FieldDescription fieldDescription)
        {

            _fieldDescription = fieldDescription;
            this.MainContainer.Orientation = Orientation.Vertical;

            // Reuse the PromptTextBox and PromptPasswordBox classes
            FieldDescription FDUserName = new FieldDescription(PowerShell.Strings.UserName) { IsMandatory = true, DefaultValue = fieldDescription.DefaultValue };
            FDUserName.SetParameterType(typeof(System.String));
            MainUserName = new PromptTextBox(FDUserName);
            MainUserName.ControlChanged += this.OnControlChanged;
            MainContainer.Children.Add(MainUserName.GetControl);

            FieldDescription FDPassword = new FieldDescription(PowerShell.Strings.Password) { IsMandatory = true };
            FDPassword.SetParameterType(typeof(System.Security.SecureString));
            MainPassword = new PromptPasswordBox(FDPassword);
            MainPassword.ControlChanged += this.OnControlChanged;
            MainContainer.Children.Add(MainPassword.GetControl);

        }

        public override void SetFocus()
        {
            MainUserName.SetFocus();
        }

        public override bool isValueOK
        {
            get
            {
                bool status = MainUserName.isValueOK && MainPassword.isValueOK;
                if (status)
                    _value = new PSCredential((string)MainUserName.Value.BaseObject, (SecureString)MainPassword.Value.BaseObject);

                return status;
            }
        }

    }

    #endregion

    #region Prompt Checkbox

    /// <summary>
    /// Create a CheckBox control for a Boolean/Switch type.
    /// </summary>
    class PromptCheckBox : PromptControl
    {
        private CheckBox MainEdit = null;

        public PromptCheckBox(FieldDescription fieldDescription)
        {
            _fieldDescription = fieldDescription;

            this.MainEdit = new CheckBox() { Content = fieldDescription.Name };

            this.MainContainer.Children.Add(this.MainEdit);

            if (_fieldDescription.DefaultValue != null && _fieldDescription.DefaultValue.BaseObject.GetType().Name == "Boolean")
                this.MainEdit.IsChecked = (bool)_fieldDescription.DefaultValue.BaseObject;

            if ((_fieldDescription.HelpMessage != null) && (!string.IsNullOrEmpty(_fieldDescription.HelpMessage)))
                this.MainEdit.ToolTip = _fieldDescription.HelpMessage;

            this.MainEdit.Checked += OnControlChanged;
            this.MainEdit.Unchecked += OnControlChanged;

        }

        public override bool isValueOK
        {
            get
            {
                _value =  MainEdit.IsChecked;
                return true;
            }
        }

        public override void SetFocus()
        {
            this.MainEdit.Focus();
            Keyboard.Focus(this.MainEdit);
        }

    }

    #endregion

    #region Prompt DateTime

    /// <summary>
    /// Create a DatePicker for a DateTime type.
    /// </summary>
    class PromptDateTime : PromptControl
    {
        private Label NameLabel = null;
        protected DatePicker MainEdit = null;

        public PromptDateTime(FieldDescription fieldDescription)
        {
            _fieldDescription = fieldDescription;

            this.NameLabel = new Label() { Content = FieldDescription.Name + ": " };
            this.MainEdit = new DatePicker();

            this.MainContainer.Children.Add(this.NameLabel);
            this.MainContainer.Children.Add(this.MainEdit);
            this.MainContainer.Children.Add(this.ErrorLabel);

            if ((_fieldDescription.DefaultValue != null) && (!string.IsNullOrEmpty(_fieldDescription.DefaultValue.BaseObject.ToString())))
                this.MainEdit.SelectedDate = (DateTime)_fieldDescription.DefaultValue.BaseObject;

            if ((_fieldDescription.HelpMessage != null) && (!string.IsNullOrEmpty(_fieldDescription.HelpMessage)))
                this.MainEdit.ToolTip = _fieldDescription.HelpMessage;

            this.MainEdit.SelectedDateChanged += OnControlChanged;
        }

        public override bool isValueOK
        {
            get
            {
                if (_fieldDescription.IsMandatory && string.IsNullOrEmpty(this.MainEdit.Text))
                    return this.EnableError(PowerShell.Strings.Missing);

                _value = this.MainEdit.DisplayDate;
                this.DisableError();
                return true;
            }
        }

        public override void SetFocus()
        {
            this.MainEdit.Focus();
            Keyboard.Focus(this.MainEdit);
        }

    }


    #endregion

    #region File and Folder picker

    /// <summary>
    /// Create a TextBox with a [...] button on the end that allows the user to select a folder or file
    /// </summary>
    class PromptFileFolderBox : PromptControl
    {
        private Label NameLabel = null;
        protected TextBox MainEdit = null;
        protected Button PickerButton = null;

        public PromptFileFolderBox( FieldDescription fieldDescription)
        {
            _fieldDescription = fieldDescription;

            this.NameLabel = new Label() { Content = FieldDescription.Name + ": " };
            this.MainEdit = new TextBox() { MinWidth = 100, Height = 20 };
            this.PickerButton = new Button() { Content = "...", Width = 20, Height = 20 };
                

            this.MainContainer.Children.Add(this.NameLabel);
            this.MainContainer.Children.Add(this.MainEdit);
            this.MainContainer.Children.Add(this.PickerButton);
            this.MainContainer.Children.Add(this.ErrorLabel);

            if ((_fieldDescription.DefaultValue != null) && (!string.IsNullOrEmpty(_fieldDescription.DefaultValue.BaseObject.ToString())))
                this.MainEdit.Text = _fieldDescription.DefaultValue.BaseObject.ToString();

            if ((_fieldDescription.HelpMessage != null) && (!string.IsNullOrEmpty(_fieldDescription.HelpMessage)))
                this.MainEdit.ToolTip = _fieldDescription.HelpMessage;

            this.MainEdit.TextChanged += OnControlChanged;
            this.MainEdit.SelectAll();

            this.PickerButton.Click += new RoutedEventHandler(delegate (object sender, RoutedEventArgs e)
            {
                if ( _fieldDescription.ParameterTypeFullName == "System.IO.FileInfo")
                {

                    Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
                    if (dlg.ShowDialog() == true)
                        this.MainEdit.Text = dlg.FileName;

                }
                else if(_fieldDescription.ParameterTypeFullName == "System.IO.DirectoryInfo")
                {
                    using (var dialog = new System.Windows.Forms.FolderBrowserDialog())  // Windows Forms, I know...
                    {
                        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                            this.MainEdit.Text = dialog.SelectedPath;
                    }
                }
                else
                {
                    MessageBox.Show("Unknown Type: " + _fieldDescription.ParameterTypeFullName);
                }
            });


        }

        public override bool isValueOK
        {
            get
            {

                if (_fieldDescription.IsMandatory && string.IsNullOrEmpty(this.MainEdit.Text))
                    return this.EnableError(PowerShell.Strings.Missing);

                if (_fieldDescription.ParameterTypeFullName == "System.IO.FileInfo")
                    _value = new System.IO.FileInfo(MainEdit.Text);
                else if (_fieldDescription.ParameterTypeFullName == "System.IO.DirectoryInfo")
                    _value = new System.IO.DirectoryInfo(MainEdit.Text);

                this.DisableError();
                return true;
            }
        }


    }

    #endregion



}
