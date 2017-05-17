using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

// PSWHost - Copyright (KeithGa@KeithGa.com) all rights reserved.
// Apache License 2.0

namespace PSWHost
{
    using System.Diagnostics;
    using System.Management.Automation;
    using System.Management.Automation.Host;
    using System.Management.Automation.Runspaces;
    using System.Windows.Markup;
    using System.Windows.Threading;
    using System.Threading;
    using System.Runtime.InteropServices;
    using System.Reflection;
    using System.IO;
    using System.Collections;
    using System.Collections.ObjectModel;

    /// <summary>
    /// PSWControl is a WPF UserControl container for our PowerShell host control.
    /// </summary>
    public partial class PSWControl : UserControl
    {

        #region Initialization

        private readonly static TraceSource MyTracer = new TraceSource("PSWControl.Main");

        private System.Management.Automation.PowerShell powershell = null;
        private PSHost PSHost = null;

        /// <summary>
        /// Object initialization
        /// </summary>
        public PSWControl()
        {
#if DEBUG
            MyTracer.Switch.Level = SourceLevels.All;
#endif
            MyTracer.TraceInformation("Initialize the Control");

            InitializeComponent();
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            {
                MyTracer.TraceInformation("Skip when this IsInDesignMode()");
                return;
            }

            /////////////////////////////////////////////////////////////////
            MyTracer.TraceInformation("Create the Powershell controller...");
            this.powershell = System.Management.Automation.PowerShell.Create();

            // Replace cmdlets with our own implementations...
            InitialSessionState iss = InitialSessionState.CreateDefault();

            // Hack Hack - Provide a PSScriptRoot for silly scripts.
            iss.Variables.Remove("PSScriptRoot", typeof(System.Management.Automation.Runspaces.SessionStateVariableEntry));
            iss.Variables.Add(new SessionStateVariableEntry("PSScriptRoot", System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "Override for the PSScriptRoot"));

            iss.Commands.Remove("Out-GridView", typeof(System.Management.Automation.Runspaces.SessionStateCmdletEntry));
            iss.Commands.Add(new SessionStateCmdletEntry("Out-GridView", typeof(HostGridView), null));
            iss.Commands.Add(new SessionStateCmdletEntry("Edit-KeyValuePair", typeof(HostGridEdit), null));
            iss.Commands.Add(new SessionStateCmdletEntry("Show-XAMLControl", typeof(ShowXAMLControl), null));
            iss.Commands.Add(new SessionStateCmdletEntry("Remove-XAMLControl", typeof(RemoveXAMLControl), null));

            powershell.Runspace = RunspaceFactory.CreateRunspace(new MyHost(this), iss);
            powershell.Runspace.ApartmentState = System.Threading.ApartmentState.STA;
            powershell.Runspace.Open();

            if (Runspace.DefaultRunspace == null)
                Runspace.DefaultRunspace = powershell.Runspace;

            // Prepare some callbacks...
            this.Loaded += new RoutedEventHandler(PSWControl_Loaded);
            powershell.InvocationStateChanged += new EventHandler<PSInvocationStateChangedEventArgs>(Powershell_InvocationStateChanged);
            powershell.Streams.Error.DataAdded += new EventHandler<DataAddedEventArgs>(delegate (object sender, DataAddedEventArgs e)
            {
                this.Parent.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                    this.WriteLabel("Red", null, ((PSDataCollection<ErrorRecord>)sender)[e.Index].ToString(), true)));
            });

        }

        #endregion

        #region Callbacks prepared during initialization

        /// <summary>
        /// Force the PSHost control to use the background color from this User Control.
        /// Unfortunately, there is no Brush to ConsoleColor conversion API, so do it here...
        /// Stolen from: http://stackoverflow.com/questions/1988833/converting-color-to-consolecolor
        /// </summary>
        private void PSWControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.Background == null)
                return;

            double delta = double.MaxValue;
            double rr = ((SolidColorBrush)this.Background).Color.R;
            double gg = ((SolidColorBrush)this.Background).Color.G;
            double bb = ((SolidColorBrush)this.Background).Color.B;

            foreach (ConsoleColor cc in Enum.GetValues(typeof(ConsoleColor)))
            {
                var n = Enum.GetName(typeof(ConsoleColor), cc);
                var c = System.Drawing.Color.FromName(n == "DarkYellow" ? "Orange" : n); // bug fix
                var t = Math.Pow(c.R - rr, 2.0) + Math.Pow(c.G - gg, 2.0) + Math.Pow(c.B - bb, 2.0);
                if (t < delta)
                {
                    delta = t;
                    this.PSHost.UI.RawUI.BackgroundColor = cc;
                    if (t == 0.0) { break; }
                }
            }

        }

        /// <summary>
        /// Called Async when the powershell host has ended.
        /// Now is the time to dump error messages to the console if the state changed to Failed.
        /// </summary>
        private void Powershell_InvocationStateChanged(object sender, PSInvocationStateChangedEventArgs e)
        {
            string ErrorMessage = null;
            if (e.InvocationStateInfo.State == PSInvocationState.Failed)
            {
                System.Management.Automation.RuntimeException r = (System.Management.Automation.RuntimeException)e.InvocationStateInfo.Reason;
                if (r.ErrorRecord.InvocationInfo != null)
                {
                    ErrorMessage = string.Format("{0} : {1}\n{2}\n+ CategoryInfo: {3}\n+ FullyQualifiedErrorId: {4}",
                        (r.ErrorRecord.InvocationInfo.MyCommand != null) ? r.ErrorRecord.InvocationInfo.MyCommand.Name : "<NULL>",
                        r.ErrorRecord.Exception.Message,
                        r.ErrorRecord.InvocationInfo.PositionMessage,
                        r.ErrorRecord.CategoryInfo,
                        r.ErrorRecord.FullyQualifiedErrorId);
                }
                else
                {
                    ErrorMessage = string.Format("{0} : {1}\n{2}\n+ CategoryInfo: {3}\n+ FullyQualifiedErrorId: {4}",
                        "<null>",
                        r.ErrorRecord.Exception.Message,
                        "<null>",
                        r.ErrorRecord.CategoryInfo,
                        r.ErrorRecord.FullyQualifiedErrorId);
                }
                if (ErrorMessage != null)
                {
                    MyTracer.TraceInformation(ErrorMessage);
                    foreach (string Line in ErrorMessage.Split('\n'))
                    {
                        // There was an error. Display here!
                        this.Parent.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => this.WriteLabel("Red", null, Line, true)));
                    }
                }

            }
            if (this.InvocationStateChanged != null)
            {
                Dispatcher.Invoke(new Action(() => this.InvocationStateChanged(this, e)));
            }
        }

        #endregion

        #region Public Events

        public event EventHandler<PSInvocationStateChangedEventArgs> InvocationStateChanged = null;

        #endregion

        #region Private Line and Character Output Managment

        /// <summary>
        /// Routines for where default output go
        /// DefaultLine - StackPanel Container from where to append new StackPanels here for new lines
        /// DefaultChar - StackPanel Container from where to append new content line Labels/TextBoxes/etc..
        /// </summary>

        private DockPanel _defaultLine = null;
        private StackPanel _defaultChar = null;

        private void AddDockChild( FrameworkElement child )
        {
            DockPanel.SetDock(child, Dock.Top);
            this.DefaultLine.Children.Add(child);
            child.BringIntoView();
        }

        private DockPanel DefaultLine
        {
            get
            {
                if (this._defaultLine == null)
                    this._defaultLine = PSWControlStart;
                return this._defaultLine;
            }
        }

        private StackPanel DefaultChar
        {
            get
            {
                if (this._defaultChar == null)
                    this.NewLine();
                return this._defaultChar;
            }
        }

        private void NewLine()
        {
            this._defaultChar = new StackPanel() { Orientation = Orientation.Horizontal };
            AddDockChild(this._defaultChar);
        }

        private DockPanel CreateNestedPanel()
        {
            return CreateNestedPanel(null);
        }

        private DockPanel CreateNestedPanel(string Shade)
        {
            this.NewLine();
            DockPanel OldDefaultLine = this.DefaultLine;
            DockPanel NewDefaultLine = new DockPanel() { Margin = new Thickness(2) }; //, LastChildFill = true };
            this.AddDockChild(NewDefaultLine);

            this._defaultLine = NewDefaultLine;

            if (!string.IsNullOrEmpty(Shade))
                DefaultLine.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom(Shade));

            this.NewLine();

            return OldDefaultLine;  // Return parent
        }

        public void PopNestedPanel(DockPanel Parent)
        {
            this._defaultLine = Parent;
            this.NewLine();
        }

        public void ClearScreen()
        {
            if (DefaultLine != null)
            {
                DefaultLine.Children.Clear();
                this.NewLine();
            }
        }

        #endregion

        #region Public Properties

        public int ExitCode
        { get; set; }

        public string Script
        { get; set; }

        public PSInvocationState GetCurrentState
        {
            get { return powershell.InvocationStateInfo.State; }
        }

        public bool isPowerShellRunning
        {
            get
            {
                switch (this.GetCurrentState)
                {
                    // case PSInvocationState.Disconnected:
                    case PSInvocationState.Stopping:
                    case PSInvocationState.Running:
                        return true;
                    case PSInvocationState.Failed:
                    case PSInvocationState.NotStarted:
                    case PSInvocationState.Completed:
                    case PSInvocationState.Stopped:
                    default:
                        return false;
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Load the embedded resource {ResourceName} and run as a powershell script.
        /// </summary>
        /// <param name="ResourceName"></param>
        public void LoadAndRunPS1FromResource()
        {
            MyTracer.TraceInformation("Load Powershell Script from embedded resource and start execution.");

            foreach (string ResourceName in Assembly.GetExecutingAssembly().GetManifestResourceNames())
            {
                if (ResourceName.ToLower().EndsWith(".ps1"))
                {
                    MyTracer.TraceInformation("Resource names {0}", ResourceName);
                    Stream DataStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName);
                    this.Script += (new StreamReader(DataStream)).ReadToEnd();
                }
            }

            this.Start(Environment.GetCommandLineArgs());

        }

        public void Start(string[] Args)
        {

            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            {
                MyTracer.TraceInformation("Start() --> Skip while in design mode");
                return;
            }

            MyTracer.TraceInformation("ScriptSize: {0}", this.Script.Length);
            MyTracer.TraceInformation("Start the script ({0})", string.Join(", ", Args));
            powershell.AddScript(this.Script);

            powershell.AddCommand("out-string");
            powershell.AddParameter("-stream");

            PSDataCollection<PSObject> output = new PSDataCollection<PSObject>();
            output.DataAdded += new EventHandler<DataAddedEventArgs>(delegate (object sender, DataAddedEventArgs e)
            {
                this.Parent.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => this.WriteLabel(null, null, output[e.Index].ToString(), true)));
            });

            IAsyncResult asyncResult = powershell.BeginInvoke<PSObject, PSObject>(null, output);

        }

        public void Stop()
        {
            MyTracer.TraceInformation("Hard Stop!");
            this.UserNavigationEventCancel(); // Flush anything waiting...
            powershell.Runspace.CloseAsync();
            powershell.BeginStop(null, null);
        }

        #endregion

        #region private XAML Processing methods

        private UIElement GenerateUIElement(string XAMLString)
        {
            ParserContext context = new ParserContext();
            context.XmlnsDictionary.Add("", "http://schemas.microsoft.com/winfx/2006/xaml/presentation");
            context.XmlnsDictionary.Add("x", "http://schemas.microsoft.com/winfx/2006/xaml");

            try
            {
                // Some processing of the XAML string when deveoped using another Visual Studio instance.
                string ReplacementXAML = (new System.Text.RegularExpressions.Regex("(x\\:Class|Title)=\"[^\"]*\"")).Replace(XAMLString, "");
                string ReplacementXAML2 = ReplacementXAML.Replace("<Window ", "<UserControl ").Replace("</Window>", "</UserControl>");
                return (System.Windows.UIElement)XamlReader.Parse(ReplacementXAML2, context);
            }
            catch
            {
                MyTracer.TraceInformation("Parse ERROR: [{0}]", XAMLString);
            }
            return null;
        }

        #endregion

        #region Label Common Functions

        public Label WriteLabel(string value)
        {
            Label newElement = new Label() { Content = value, Padding = new Thickness(0) };

            DefaultChar.Children.Add(newElement);
            return newElement;
        }

        public Label WriteLabel(string foregroundColor, string backgroundColor, string value, bool newLine)
        {
            Label newElement = WriteLabel(value);
            if (!string.IsNullOrEmpty(foregroundColor)) { newElement.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom(foregroundColor)); }
            if (!string.IsNullOrEmpty(backgroundColor)) { newElement.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom(backgroundColor)); }
            if (newLine)
                this.NewLine();
            return newElement;
        }

        #endregion

        #region Callbacks for PowerShell Host Control

        public void ShowPassword()
        {

            PasswordBox newElement = new PasswordBox() { MinWidth = 100 };
            newElement.PasswordChanged += new RoutedEventHandler(delegate (object sender, RoutedEventArgs e)
            {
                MyTracer.TraceInformation("Validate the TextBox");
                var secure = new System.Security.SecureString();
                foreach (char c in ((PasswordBox)sender).Password)
                {
                    secure.AppendChar(c);
                }

                xResult = new PSObject(secure);
            });

            newElement.KeyDown += new KeyEventHandler(delegate (object sender, KeyEventArgs e)
            {
                if (e.Key == Key.Return)
                {
                    MyTracer.TraceInformation("User pressed ENTER!");
                    this.UserNavigationEventOK();
                }
            });
            DefaultChar.Children.Add(newElement);
            newElement.Focus();
            Keyboard.Focus(newElement);
            this.ReadyForUserNavigation(newElement);
            this.NewLine();
        }

        public void ShowReadLine()
        {
            if (DefaultChar == null) { MyTracer.TraceEvent(TraceEventType.Error, 101, "DefaultChar is NULL."); }

            TextBox newElement = new TextBox() { MinWidth = 100 };
            newElement.TextChanged += new TextChangedEventHandler(delegate (object sender, TextChangedEventArgs e)
            {
                MyTracer.TraceInformation("Read the TextBox");
                xResult = new PSObject(((TextBox)sender).Text);
            });

            newElement.KeyDown += new KeyEventHandler(delegate (object sender, KeyEventArgs e)
            {
                if (e.Key == Key.Return)
                {
                    MyTracer.TraceInformation("User pressed ENTER!");
                    this.UserNavigationEventOK();
                }
            });
            DefaultChar.Children.Add(newElement);
            newElement.Focus();
            Keyboard.Focus(newElement);
            this.ReadyForUserNavigation(newElement);
            this.NewLine();
        }

        [DllImport("user32.dll")]
        public static extern uint MapVirtualKey(uint uCode, uint uMapType);

        public void ReadKey(ReadKeyOptions options)
        {
            Application.Current.MainWindow.KeyDown += MainWindow_KeyDown;
            this.ReadyForUserNavigation(null);
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            int vk = KeyInterop.VirtualKeyFromKey(e.Key);
            char c = (char)MapVirtualKey((uint)vk, 2);
            MyTracer.TraceInformation("User pressed the ANY KEY [{0}]!", c);
            xResult = new PSObject(new KeyInfo(vk, c, 0, e.IsDown));
            this.UserNavigationEventOK();
        }

        public void WriteProgress(long SourceId, ProgressRecord record)
        {
            string MySourceId = "0";
            if (record.ParentActivityId >= 0)
            {
                MySourceId = string.Format("{0}_{1}", record.ActivityId, record.ParentActivityId);
            }
            else
            {
                MySourceId = string.Format("{0}", record.ActivityId);
            }

            StackPanel FoundGroup = (StackPanel)LogicalTreeHelper.FindLogicalNode(this, string.Format("ProgressGroup_{0}", MySourceId));
            Label foundlabel = null;
            ProgressBar progress = null;

            if (record.RecordType == ProgressRecordType.Completed)
            {
                if (FoundGroup != null)
                {
                    // Remove group
                    DependencyObject parent = VisualTreeHelper.GetParent(FoundGroup);
                    if (parent != null)
                        ((DockPanel)parent).Children.Remove(FoundGroup);
                }
                return;
            }

            if (FoundGroup == null)
            {
                //Create Group!

                string XAMLStream = string.Format(@"
                    <StackPanel x:Name='ProgressGroup_{0}' Orientation='Vertical' Margin='5' Width='Auto' Background='{1}' >
                            <Label x:Name='ProgressMainLabel_{0}' FontWeight='Bold' Visibility='Visible'/>
                            <Label x:Name='ProgressStatusLabel_{0}' Content='Sub Label' Visibility='Visible'/>
                            <ProgressBar x:Name='ProgressMain_{0}' Height='15' Value='40' Visibility='Collapsed' />
                            <Label x:Name='ProgressSubLabel_{0}' Visibility='Collapsed'/>
                    </StackPanel>",
                    MySourceId, PowerShell.Strings.WhiteSmoke);
                StackPanel newElement = (StackPanel)this.GenerateUIElement(XAMLStream);
                if (newElement == null) { MyTracer.TraceEvent(TraceEventType.Error, 101, "ProgressBar Element not created! "); }

                AddDockChild(newElement);
                this.NewLine();

            }

            if ((foundlabel = (Label)LogicalTreeHelper.FindLogicalNode(this, string.Format("ProgressMainLabel_{0}", MySourceId))) != null)
            {
                if (!string.IsNullOrWhiteSpace(record.Activity))
                {
                    foundlabel.Content = record.Activity;
                    foundlabel.Visibility = Visibility.Visible;
                }
            }
            if ((foundlabel = (Label)LogicalTreeHelper.FindLogicalNode(this, string.Format("ProgressStatusLabel_{0}", MySourceId))) != null)
            {
                if (!string.IsNullOrWhiteSpace(record.StatusDescription))
                {
                    foundlabel.Content = record.StatusDescription;
                    foundlabel.Visibility = Visibility.Visible;
                }
            }
            if ((foundlabel = (Label)LogicalTreeHelper.FindLogicalNode(this, string.Format("ProgressSubLabel_{0}", MySourceId))) != null)
            {
                if (!string.IsNullOrWhiteSpace(record.CurrentOperation))
                {
                    foundlabel.Content = record.CurrentOperation;
                    foundlabel.Visibility = Visibility.Visible;
                }
            }

            if ((progress = (ProgressBar)LogicalTreeHelper.FindLogicalNode(this, string.Format("ProgressMain_{0}", MySourceId))) != null)
            {
                if (record.PercentComplete >= 0 && record.PercentComplete <= 100)
                {
                    progress.Value = record.PercentComplete;
                    progress.Visibility = Visibility.Visible;
                }
            }

            return;
        }

        public void PromptForChoice(string caption, string message, System.Collections.ObjectModel.Collection<ChoiceDescription> choices, int defaultChoice)
        {
            // For PromptForChoice we will use CommandLinks - Closest UI Paradigm
            // Future: XXX TBD - also allow for Radio Buttons

            MyTracer.TraceInformation("Prompt for Choice {0} {1}", caption, message);

            DockPanel OldDefaultLine = this.CreateNestedPanel(PowerShell.Strings.WhiteSmoke);
            this.WriteLabel(null, null, caption, true).FontWeight = FontWeights.Bold;
            this.WriteLabel(message);
            this.NewLine();

            int Index = 0;
            foreach (var Item in choices)
            {
                MyTracer.TraceInformation("NewChoiceDescription: {0} {1}", Item.Label, Item.HelpMessage);

                CommandLink NewCmdLink = new CommandLink()
                { HorizontalAlignment = HorizontalAlignment.Stretch, Margin = new Thickness(5), Link = Item.Label };

                NewCmdLink.Tag = Index++;
                var uriSource = new Uri(string.Format(@"/{0};component/PowerShell/arrow-blue.png", Assembly.GetExecutingAssembly().EntryPoint.DeclaringType.Namespace), UriKind.Relative);
                NewCmdLink.Icon = new BitmapImage(uriSource);
                if (!string.IsNullOrEmpty(Item.HelpMessage)) { NewCmdLink.Note = Item.HelpMessage; }
                NewCmdLink.Click += new RoutedEventHandler(delegate (object sender, RoutedEventArgs e)
                {
                    xResult = new PSObject(((CommandLink)((RadioButton)sender).Parent).Tag);
                    this.UserNavigationEventOK();
                });

                DockPanel.SetDock(NewCmdLink, Dock.Top);
                DefaultChar.Children.Add(NewCmdLink);
                ((FrameworkElement)NewCmdLink).BringIntoView();
                this.NewLine();

            }

            DockPanel PromptPanel = DefaultLine;
            this.PopNestedPanel(OldDefaultLine);
            this.ReadyForUserNavigation((DockPanel)PromptPanel);

        }

        #endregion

        #region Callbacks for PowerShell Host Control function Prompt()

        public void Prompt(string caption, string message, System.Collections.ObjectModel.Collection<FieldDescription> descriptions)
        {
            List<PromptControl> outputList = new List<PromptControl>();
            bool isFirst = true;

            MyTracer.TraceInformation("Prompt {0} {1}", caption, message);

            DockPanel OldDefaultLine = this.CreateNestedPanel(PowerShell.Strings.WhiteSmoke);
            this.WriteLabel(null, null, caption, true).FontWeight = FontWeights.Bold;
            this.WriteLabel(message);
            this.NewLine();

            ///////////////////////////////////////
            // Handle the NEXT and Cancel buttons

            bool NextCancelCreated = CreateNextCancelIfMissing();

            this.CancelButton.Click += new RoutedEventHandler(delegate (object sender, RoutedEventArgs e)
            {
                this.UserNavigationEventCancel();
            });

            this.NextButton.Click += new RoutedEventHandler(delegate (object sender, RoutedEventArgs e)
            {
                MyTracer.TraceInformation("User pressed Next");

                Dictionary<string, PSObject> dResults = new Dictionary<string, PSObject>();
                foreach (PromptControl outitem in outputList)
                {
                    if (outitem.Value != null)
                        dResults.Add(outitem.FieldDescription.Name, outitem.Value);
                }

                xResult = new PSObject(dResults);
                this.UserNavigationEventOK();
            });

            ///////////////////////////////////////

            foreach (var FieldDesc in descriptions)
            {
                string BaseType = FieldDesc.ParameterTypeFullName;

                MyTracer.TraceInformation("\t\tNewFieldDescription: Name:{0} Label:{1} Mandatory:{2} Type:{3} Default:[{4}] Attr: {5}",
                    FieldDesc.Name, FieldDesc.Label, FieldDesc.IsMandatory, FieldDesc.ParameterTypeFullName, FieldDesc.DefaultValue, FieldDesc.Attributes.ToString());

                PromptControl newElement = null;

                switch (BaseType)
                {

                    case "System.IO.FileInfo":
                    case "System.IO.DirectoryInfo":
                        newElement = new PromptFileFolderBox(FieldDesc);
                        break;

                    case "System.String[]":
                        newElement = new PromptMultiLineTextBox(FieldDesc);
                        break;

                    case "System.Boolean":
                    case "System.Management.Automation.SwitchParameter":
                        newElement = new PromptCheckBox(FieldDesc);
                        break;

                    case "System.Management.Automation.PSCredential":
                        newElement = new PromptCredentialBox(FieldDesc);
                        break;

                    case "System.Security.SecureString":
                        newElement = new PromptPasswordBox(FieldDesc);
                        break;

                    case "System.DateTime":
                        newElement = new PromptDateTime(FieldDesc);
                        break;

                    case "System.Guid":
                    case "System.SByte":
                    case "System.Byte":
                    case "System.Char":
                    case "System.Decimal":
                    case "System.Double":
                    case "System.Int16":
                    case "System.Int32":
                    case "System.Int64":
                    case "System.Single":
                    case "System.UInt16":
                    case "System.UInt32":
                    case "System.UInt64":
                    case "System.UIntPtr":
                    case "System.String":
                        newElement = new PromptTextBox(FieldDesc);
                        break;

                    default:
                        string Message = string.Format("Unhandled Type: {0}", FieldDesc.ParameterTypeFullName);
                        MyTracer.TraceInformation(Message);
                        Label MyDefault = WriteLabel(Message);
                        MyDefault.FontWeight = FontWeights.Bold;
                        break;

                }

                if (newElement != null)
                {
                    this.AddDockChild(newElement.GetControl);
                    outputList.Add(newElement);

                    newElement.ControlChanged += new RoutedEventHandler(delegate (object sender, RoutedEventArgs e)
                    {
                        MyTracer.TraceInformation("Control changed ...");
                        NextButton.IsEnabled = true;
                        foreach (PromptControl outitem in outputList)
                            if (!outitem.isValueOK)
                                NextButton.IsEnabled = false;
                    });

                    this.NewLine();
                    if (isFirst)
                    {
                        newElement.SetFocus();
                        isFirst = false;
                    }
                }
            }

            ///////////////////////////////////////
            // process all the controls for the initial view.
            NextButton.IsEnabled = true;
            foreach (PromptControl outitem in outputList)
                if (!outitem.isValueOK)
                    NextButton.IsEnabled = false;

            ///////////////////////////////////////

            this.NewLine();
            if(NextCancelCreated)
                ShowNextCancelIfMissing();

            DockPanel PromptPanel = DefaultLine;
            this.PopNestedPanel(OldDefaultLine);
            this.ReadyForUserNavigation((DockPanel)PromptPanel);

        }

        #endregion

        #region GridView and GridEdit

        public void FinishedGridView(IEnumerable objects, bool ShouldWait, bool MultiSelect, bool ShowHeader)
        {
            MyTracer.TraceInformation("ShowGridView {0} {1} {2}", ShouldWait, MultiSelect, ShowHeader);

            DockPanel OldDefaultLine = this.CreateNestedPanel(PowerShell.Strings.WhiteSmoke);

            DataGrid newElement = new DataGrid()
            {
                IsReadOnly = true,
                AutoGenerateColumns = true,
                SelectionMode = MultiSelect ? DataGridSelectionMode.Extended : DataGridSelectionMode.Single,
                HeadersVisibility = ShowHeader ? DataGridHeadersVisibility.All : DataGridHeadersVisibility.None,
                SelectionUnit = DataGridSelectionUnit.FullRow,
                //ColumnWidth = DataGridLength.Auto,
                Margin = new Thickness(5, 5, 10, 5),
                GridLinesVisibility = DataGridGridLinesVisibility.None
            };

            int ColumnCount = 0;
            newElement.AutoGeneratingColumn += new EventHandler<DataGridAutoGeneratingColumnEventArgs>(delegate (object sender, DataGridAutoGeneratingColumnEventArgs e)
            {
                if (!ShowHeader)
                    e.Column.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
                if (ColumnCount++ >= 10)
                    e.Cancel = true; // Trim all columns after 10 (too busy)
            });

            newElement.ItemsSource = objects;
            bool NextCancelCreated = false;
            if (ShouldWait)
            {
                NextCancelCreated = CreateNextCancelIfMissing();
                newElement.SelectionChanged += new SelectionChangedEventHandler(delegate (object sender, SelectionChangedEventArgs e)
                {
                    MyTracer.TraceInformation("Selection changed ...");

                    List<int> MySelectedIndex = new List<int>();
                    for (int i = 0; i < newElement.Items.Count; i++)
                        foreach (var item in newElement.SelectedItems)
                            if (newElement.Items[i] == item)
                                MySelectedIndex.Add(i);

                    this.xResult = new PSObject(MySelectedIndex);
                    NextButton.IsEnabled = newElement.SelectedItems.Count > 0;
                });
                newElement.MouseDoubleClick += new MouseButtonEventHandler(delegate (object sender, MouseButtonEventArgs e)
                {
                    MyTracer.TraceInformation("User selected item though double click");
                    this.UserNavigationEventOK();
                });
            }

            this.NewLine();
            AddDockChild(newElement); 
            this.NewLine();

            ///////////////////////////////////////////

            if (ShouldWait)
            {

                NextButton.IsEnabled = false;

                this.CancelButton.Click += new RoutedEventHandler(delegate (object sender, RoutedEventArgs e)
                {
                    MyTracer.TraceInformation("User pressed Cancel");
                    this.UserNavigationEventCancel();
                });

                this.NextButton.Click += new RoutedEventHandler(delegate (object sender, RoutedEventArgs e)
                {
                    MyTracer.TraceInformation("User pressed Next");
                    this.UserNavigationEventOK();
                });

                if (NextCancelCreated)
                    ShowNextCancelIfMissing();

            }

            ///////////////////////////////////////////

            DockPanel PromptPanel = DefaultLine;
            this.PopNestedPanel(OldDefaultLine);

            if (ShouldWait)
                this.ReadyForUserNavigation((DockPanel)PromptPanel);

        }

        public IEnumerable<DataGridRow> GetDataGridRows(DataGrid grid)
        {
            var itemsSource = grid.ItemsSource as IEnumerable;
            if (null == itemsSource) yield return null;
            foreach (var item in itemsSource)
            {
                var row = grid.ItemContainerGenerator.ContainerFromItem(item) as DataGridRow;
                if (null != row) yield return row;
            }
        }

        public void FinishedGridEdit(ArrayList objects, int[] HeaderWidths)
        {

            MyTracer.TraceInformation("ShowGridView-Edit ");

            DockPanel OldDefaultLine = this.CreateNestedPanel(PowerShell.Strings.WhiteSmoke);

            DataGrid newElement = new DataGrid()
            {
                AutoGenerateColumns = true,
                SelectionMode = DataGridSelectionMode.Single,
                HeadersVisibility = DataGridHeadersVisibility.All,
                SelectionUnit = DataGridSelectionUnit.Cell,
                //ColumnWidth = DataGridLength.Auto,
                Margin = new Thickness(5, 5, 10, 5),
                GridLinesVisibility = DataGridGridLinesVisibility.All,
            };
            newElement.VerticalAlignment = VerticalAlignment.Stretch;

            newElement.ItemsSource = objects;

            this.NewLine();
            AddDockChild(newElement);
            this.NewLine();


            int FoundToolTip = -1;
            int ColumnCount = 0;
            newElement.AutoGeneratingColumn += new EventHandler<DataGridAutoGeneratingColumnEventArgs>(delegate (object sender, DataGridAutoGeneratingColumnEventArgs e)
            {
                if (HeaderWidths != null)
                {
                    if (HeaderWidths[ColumnCount] == 0)
                    {
                        e.Cancel = true;
                        ColumnCount++;
                        return;
                    }
                    else
                    {
                        if (Math.Abs(HeaderWidths[ColumnCount]) < 10)
                            e.Column.Width = new DataGridLength(Math.Abs(HeaderWidths[ColumnCount]), DataGridLengthUnitType.Star);
                        else
                            e.Column.Width = new DataGridLength(Math.Abs(HeaderWidths[ColumnCount]), DataGridLengthUnitType.Pixel);
                        e.Column.IsReadOnly = (HeaderWidths[ColumnCount] < 0);
                    }

                    if (e.PropertyName.ToLower() == "tooltip")
                        FoundToolTip = ColumnCount;
                }

                if (ColumnCount++ >= 10)
                    e.Cancel = true; // Trim all columns after 10 (too busy)
            });

            newElement.LoadingRow += new EventHandler<DataGridRowEventArgs>(delegate (object sender, DataGridRowEventArgs e)
            {
                try
                {
                    e.Row.ToolTip = ((PSObject)e.Row.Item).Properties["ToolTip"].Value;
                } catch { }
            });

            ///////////////////////////////////////////

            bool NextCancelCreated = CreateNextCancelIfMissing();

            this.CancelButton.Click += new RoutedEventHandler(delegate (object sender, RoutedEventArgs e)
            {
                MyTracer.TraceInformation("User pressed Cancel");
                this.UserNavigationEventCancel();
            });

            this.NextButton.Click += new RoutedEventHandler(delegate (object sender, RoutedEventArgs e)
            {
                MyTracer.TraceInformation("User pressed Next");
                List<PSObject> newResult = new List<PSObject>();
                foreach (DataGridRow single_row in GetDataGridRows(newElement))
                {
                    newResult.Add(new PSObject(single_row.DataContext));
                }
                this.xResult = new PSObject(newResult);
                this.UserNavigationEventOK();
            });

            if (NextCancelCreated)
                ShowNextCancelIfMissing();

            ///////////////////////////////////////////

            DockPanel PromptPanel = DefaultLine;
            this.PopNestedPanel(OldDefaultLine);

            this.ReadyForUserNavigation((DockPanel)PromptPanel);

        }


        #endregion

        #region Callbacks for XAML controls

        string g_WaitForUserScriptBlock = null;

        public void RemoveXAMLControl ( RemoveXAMLControl theCmdlet)
        {
            MyTracer.TraceInformation("RemoveXAMLControl()");
            RemoveControlAndRefactor(theCmdlet.InputObject);

        }

        public void DisplayXAMLControl (ShowXAMLControl theCmdlet )
        {
            DockPanel OldDefaultLine = null;
            MyTracer.TraceInformation("DisplayXAMLControl()" );

            UIElement newElement = this.GenerateUIElement(theCmdlet.XAMLString);

            if (theCmdlet.FullScreen.IsPresent)
                this.FullScreen.Children.Add(newElement);
            else
            {
                OldDefaultLine = this.CreateNestedPanel(theCmdlet.WaitForUser?PowerShell.Strings.WhiteSmoke:null);
                this.DefaultChar.Children.Add(newElement);

            }

            //////////////////////////////////////////////////////////
            if ( theCmdlet.NewStdOut != null )
            {
                MyTracer.TraceInformation("\t\tRemap StdOut to the new dockpanel: {0} ", theCmdlet.NewStdOut);

                // remap stdout to the new DockPanel
                DockPanel FoundStdOut = (DockPanel)LogicalTreeHelper.FindLogicalNode(this, theCmdlet.NewStdOut);
                if ( FoundStdOut != null )
                {
                    if ( FoundStdOut.GetType().Name == "DockPanel" )
                    {
                        this._defaultLine = FoundStdOut;
                        this.NewLine();
                    }
                }
                else
                {
                    MyTracer.TraceInformation("Unable to find tree item {0}", theCmdlet.NewStdOut);
                }
            }

            //////////////////////////////////////////////////////////
            // Handle Next/Cancel buttons. 
            foreach (var btn in VisualControl.FindLogicalChildren<Button>(newElement))
            {
                // Found a Button
                if (btn.IsDefault)
                {
                    MyTracer.TraceInformation("Next Button found! Override!!!");
                    this._NextButton = btn;
                }
                else if (btn.IsCancel)
                {
                    MyTracer.TraceInformation("Cancel Button found! Override!!!");
                    this._CancelButton = btn;
                }
            }

            //////////////////////////////////////////////////////////
            if (theCmdlet.WaitForUser)
            {

                if (theCmdlet.Stopping)
                    return;

                g_WaitForUserScriptBlock = theCmdlet.ScriptBlock.ToString();

                #region Slam the variables and events into the controls
                //////////////////////////////////////////////////////////
                // find all controls with a "Name" defined. 

                Collection<PSObject> parentfunctions = null;
                if ( !string.IsNullOrEmpty(g_WaitForUserScriptBlock) )
                {
                    // Get all function names from the scriptblock
                    System.Management.Automation.PowerShell LocalPowershell = System.Management.Automation.PowerShell.Create();
                    LocalPowershell.AddScript(theCmdlet.ScriptBlock.ToString());
                    LocalPowershell.AddScript("get-childitem -path function:");
                    parentfunctions = LocalPowershell.Invoke();
                }

                // Enumerate through all controls...
                foreach (var ctrl in VisualControl.FindLogicalChildren<Control>(newElement))
                {

                    MyTracer.TraceInformation("\t\tControl found! {0} ", ctrl.GetType().FullName);

                    #region DefaultValues

                    if ( !string.IsNullOrEmpty(ctrl.Name) && theCmdlet.DefaultValues != null)
                    {

                        // Find all associated variables and auto load into form
                        if (theCmdlet.DefaultValues.ContainsKey(ctrl.Name) )
                        {
                            var value = theCmdlet.DefaultValues[ctrl.Name];
                            switch ( ctrl.GetType().Name )
                            {
                                case "Label":
                                    ((Label)ctrl).Content = value.ToString();
                                    break;

                                case "TextBox":
                                    ((TextBox)ctrl).Text = value.ToString();
                                    break;

                                case "PasswordBox":
                                    ((PasswordBox)ctrl).Password = value.ToString();
                                    break;

                                case "RadioButton":
                                    ((RadioButton)ctrl).IsChecked = (value.ToString().ToLower() == true.ToString().ToLower()) || (value.ToString().ToLower() == "checked");
                                    break;

                                case "CheckBox":
                                    ((CheckBox)ctrl).IsChecked = (value.ToString().ToLower() == true.ToString().ToLower()) || (value.ToString().ToLower() == "checked");
                                    break;

                                case "DatePicker":
                                    ((DatePicker)ctrl).SelectedDate = DateTime.Parse(value.ToString());
                                    break;

                                case "Slider":
                                    try { ((Slider)ctrl).Value = Int32.Parse(value.ToString()); } catch { }
                                    break;

                                case "ComboBox":
                                    try
                                    { ((ComboBox)ctrl).SelectedIndex = Int32.Parse(value.ToString()); } catch { }
                                    break;

                                case "ListBox":
                                    try { ((ListBox)ctrl).SelectedIndex = Int32.Parse(value.ToString()); } catch { }
                                    break;

                                default:
                                    MyTracer.TraceInformation("Control unknown!");
                                    break;

                            }

                        }
                    }

                    #endregion

                    #region Add Event Handler

                    if ( parentfunctions != null )
                    {
                        foreach (var testfn in parentfunctions)
                        {
                            if (testfn.BaseObject.ToString().StartsWith(ctrl.Name + "_", StringComparison.CurrentCultureIgnoreCase))
                            {
                                foreach (var evt in ctrl.GetType().GetEvents())
                                {
                                    if (testfn.BaseObject.ToString().ToLower() == (ctrl.Name + "_" + evt.Name).ToLower())
                                    {
                                        MyTracer.TraceInformation("\t\tEVENT found! {0} ", testfn.BaseObject.ToString());

                                        Delegate handler = Delegate.CreateDelegate(evt.EventHandlerType, this, GetMethodInfo(this.HandleUIElementEvent));
                                        evt.AddEventHandler(ctrl, handler);
                                    }
                                }
                            }
                        }
                    }

                    #endregion

                }

                #endregion

                #region Next and Cancel Controls...
                //////////////////////////////////////////////////////////

                MyTracer.TraceInformation("\t\tHandle Next and Cancel");

                bool NextCancelCreated = CreateNextCancelIfMissing();

                this.CancelButton.Click += new RoutedEventHandler(delegate (object sender, RoutedEventArgs e)
                {
                    MyTracer.TraceInformation("User pressed Cancel");
                    this.UserNavigationEventCancel();
                });

                this.NextButton.Click += new RoutedEventHandler(delegate (object sender, RoutedEventArgs e)
                {
                    MyTracer.TraceInformation("User pressed Next...read all controls and send to stdout");

                    Dictionary<string, PSObject> dResults = new Dictionary<string, PSObject>();
                    foreach (var ctrl in VisualControl.FindLogicalChildren<Control>(newElement))
                    {
                        if (!string.IsNullOrEmpty(ctrl.Name))
                        {
                            MyTracer.TraceInformation("\t\tControl found! {0} ", ctrl.GetType().FullName);

                            switch (ctrl.GetType().Name)
                            {
                                case "Label":
                                    dResults.Add(ctrl.Name, new PSObject(((Label)ctrl).Content));
                                    break;

                                case "TextBox":
                                    dResults.Add(ctrl.Name, new PSObject(((TextBox)ctrl).Text));
                                    break;

                                case "PasswordBox":
                                    dResults.Add(ctrl.Name, new PSObject(((PasswordBox)ctrl).SecurePassword));
                                    break;

                                case "RadioButton":
                                    dResults.Add(ctrl.Name, new PSObject(((RadioButton)ctrl).IsChecked.ToString()));
                                    break;

                                case "CheckBox":
                                    dResults.Add(ctrl.Name, new PSObject(((CheckBox)ctrl).IsChecked.ToString()));
                                    break;

                                case "DatePicker":
                                    dResults.Add(ctrl.Name, new PSObject(((DatePicker)ctrl).SelectedDate.ToString()));
                                    break;

                                case "Slider":
                                    dResults.Add(ctrl.Name, new PSObject(((Slider)ctrl).Value.ToString()));
                                    break;

                                case "ComboBox":
                                    dResults.Add(ctrl.Name, new PSObject(((ComboBox)ctrl).SelectedItem));
                                    break;

                                case "ListBox":
                                    dResults.Add(ctrl.Name, new PSObject(((ListBox)ctrl).SelectedItem));
                                    break;

                                default:
                                    MyTracer.TraceInformation("Control unknown!");
                                    break;

                            }
                        }
                    }

                    xResult = new PSObject(dResults);
                    this.UserNavigationEventOK();
                });

                if (NextCancelCreated)
                    ShowNextCancelIfMissing();

                #endregion

            }

            if (theCmdlet.WaitForUser)
            {
                if (OldDefaultLine == null)
                {
                    ReadyForUserNavigation(newElement);
                }
                else
                {
                    DockPanel PromptPanel = DefaultLine;
                    this.PopNestedPanel(OldDefaultLine);
                    this.ReadyForUserNavigation((DockPanel)PromptPanel);
                }

            }
            else // if ( theCmdlet.PassThru.IsPresent )
            {
                this.ReadyForUserNavigation(null);
                xResult = new PSObject(newElement);
                this.UserNavigationEventOK();
            }


        }

        public  void HandleUIElementEvent(object sender, RoutedEventArgs e)
        {
            MyTracer.TraceInformation("HandleUIELement  {0}_{1} ", ((Control)sender).Name, e.RoutedEvent.Name );

            // Construct a PowerShell Script http://stackoverflow.com/questions/4179351/calling-powershell-functions-from-c-sharp

            System.Management.Automation.PowerShell LocalPowershell = System.Management.Automation.PowerShell.Create();

            LocalPowershell.Runspace = RunspaceFactory.CreateRunspace();
            LocalPowershell.Runspace.ThreadOptions = PSThreadOptions.UseCurrentThread;
            LocalPowershell.Runspace.Open();

            if (string.IsNullOrEmpty(g_WaitForUserScriptBlock))
                throw new System.NullReferenceException();
            LocalPowershell.AddScript(g_WaitForUserScriptBlock);

            foreach (var ctrl in VisualControl.FindLogicalChildren<Control>(this))  // RequestedControlToCloseOnNext
            {
                //MyTracer.TraceInformation("\t\tControl found! {0} ", ctrl.Name);
                if (!string.IsNullOrEmpty(ctrl.Name))
                    LocalPowershell.AddCommand("Set-variable").AddParameter("Name", ctrl.Name).AddParameter("Value", ctrl).AddParameter("Force");
            }
            Collection<PSObject> results1 = LocalPowershell.Invoke();
            LocalPowershell.Commands.Clear();

            // Call the function...
            LocalPowershell.AddCommand(string.Format("{0}_{1}", ((Control)sender).Name, e.RoutedEvent.Name)).AddArgument(sender).AddArgument(e);
            Collection<PSObject> results2 = LocalPowershell.Invoke();

            MyTracer.TraceInformation("HandleUIELement Done  {0}_{1} ", results1.Count, results2.Count);

        }

        private MethodInfo GetMethodInfo(Action<object, RoutedEventArgs> handleUIElementEvent)
        {
            return handleUIElementEvent.Method;
        }

        #endregion

        #region Next and Cancel Buttons

        private Button _NextButton = null;
        private Button _CancelButton = null;

        private Button NextButton
        {
            get
            {
                if (_NextButton == null)
                    throw new System.NullReferenceException("Get Next Button");
                return this._NextButton;
            }
        }
        private Button CancelButton
        {
            get
            {
                if (_CancelButton == null)
                    throw new System.NullReferenceException("Get Cancel Button");
                return this._CancelButton;
            }
        }

        private bool CreateNextCancelIfMissing()
        {
            bool result = false;
            if (_NextButton == null)
            {
                this._NextButton = new Button() { Content = "Next >", Width = 75, Margin = new Thickness(10), IsDefault = true };
                result = true;
            }
            if (_CancelButton == null)
            {
                this._CancelButton = new Button() { Content = "Cancel", Width = 75, Margin = new Thickness(10), IsCancel = true };
                result = true;
            }
            return result;
        }

        private void ShowNextCancelIfMissing()
        {
            this.NewLine();
            if (_NextButton != null)
                this.DefaultChar.Children.Add(this.NextButton);
            if (_CancelButton != null)
                this.DefaultChar.Children.Add(this.CancelButton);
            this.NewLine();
        }

        #endregion

        #region User Navigation Syncronization

        /// <summary>
        /// These syncrhonization methods handle simple (OK/Cancel) UI Navigation when the PowerShell host requests User Interaction.
        /// </summary>

        private EventWaitHandle mWaitForUserNavigation = new EventWaitHandle(false, EventResetMode.AutoReset);
        private bool isUserNavigationCanceled = false;
        private PSObject xResult = null;
        private UIElement RequestedControlToCloseOnNext = null;

        /// <summary>
        /// A UI element was placed on the sceen that requires async user Navigation (Like a Next Button).
        /// </summary>
        /// <param name="ControlToClose">Optional control to close when UserNavigationEvent has occured.</param>
        public void ReadyForUserNavigation(UIElement ControlToClose)
        {
            MyTracer.TraceInformation("ReadyForUserNavigation - ");
            RequestedControlToCloseOnNext = ControlToClose;
            mWaitForUserNavigation.Reset();
            isUserNavigationCanceled = false;
            xResult = null;
        }

        /// <summary>
        /// User initiated navigation event - Cancel
        /// </summary>
        public void UserNavigationEventCancel()
        {
            MyTracer.TraceInformation("UserNavigationEventCancel - Signal to continue...");
            if ( ! mWaitForUserNavigation.WaitOne(0) )
            {
                MyTracer.TraceInformation("Not waiting for anything do Stop()...");
                powershell.Runspace.CloseAsync();
                powershell.BeginStop(null, null);
                mWaitForUserNavigation.Set();
                return;
            }
            isUserNavigationCanceled = true;
            mWaitForUserNavigation.Set();
            RemoveControlAndRefactor(RequestedControlToCloseOnNext);
        }

        /// <summary>
        /// User initiated navigation event - OK
        /// </summary>
        public void UserNavigationEventOK()
        {
            MyTracer.TraceInformation("UserNavigationEventOK - Signal to continue...");
            mWaitForUserNavigation.Set();
            RemoveControlAndRefactor(RequestedControlToCloseOnNext);
        }


        private void RemoveControlAndRefactor(UIElement ControlToRemove)
        {
            if (ControlToRemove == null)
            {
                Application.Current.MainWindow.KeyDown -= MainWindow_KeyDown;
                return;
            }

            MyTracer.TraceInformation("Remove Control");
            DependencyObject parent = VisualTreeHelper.GetParent(ControlToRemove);
            if (Parent != null)
                if (((Panel)parent).Children.Contains(ControlToRemove))
                    ((Panel)parent).Children.Remove(ControlToRemove);

            g_WaitForUserScriptBlock = null;

            MyTracer.TraceInformation("Refactor controls");
            bool FoundNext = false;
            bool FoundCancel = false;
            bool FoundLine = false;

            foreach (var ctrl in VisualControl.FindLogicalChildren<Button>(this))
            {
                if (ctrl.Equals(this._NextButton))
                    FoundNext = true;
                else if (ctrl.Equals(this._CancelButton))
                    FoundCancel = true;
                if (FoundNext && FoundCancel)
                    break;
            }

            foreach (var ctrl in VisualControl.FindLogicalChildren<DockPanel>(this))
            {
                if (ctrl.Equals(this._defaultLine))
                {
                    FoundLine = true;
                    break;
                }
            }

            if (!FoundLine)
            {
                MyTracer.TraceInformation("Reset the Line");
                this._defaultLine = PSWControlStart;
                this.ClearScreen();
            }

            if (!FoundCancel)
            {
                MyTracer.TraceInformation("Reset Cancel Button");
                this._CancelButton = null;
                this.ClearScreen();
            }
            else
            {
                // this._CancelButton.Click += null;
            }

            if (!FoundNext)
            {
                MyTracer.TraceInformation("Reset Next Button");
                this._NextButton = null;
                this.ClearScreen();
            }
            else
            {
                // this._NextButton.Click += null;
            }

        }

        /// <summary>
        /// Another thread is waiting for a UI event to be triggered.
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public bool WaitForUserNavigationEvent(out PSObject result)
        {
            MyTracer.TraceInformation("WaitForUserNavigationEvent - Wait for UserInput... result");
            mWaitForUserNavigation.WaitOne();
            MyTracer.TraceInformation("WaitForUserNavigationEvent - Ready to continue... result");
            result = xResult;
            return (xResult != null && !isUserNavigationCanceled);
        }

        #endregion

    }

    #region Static Support classes

    public static class VisualControl
    {
        public static IEnumerable<T> FindLogicalChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                foreach (object rawChild in LogicalTreeHelper.GetChildren(depObj))
                {
                    if (rawChild is DependencyObject)
                    {
                        DependencyObject child = (DependencyObject)rawChild;
                        if (child is T)
                        {
                            yield return (T)child;
                        }

                        foreach (T childOfChild in FindLogicalChildren<T>(child))
                        {
                            yield return childOfChild;
                        }
                    }
                }
            }
        }

        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        public static IEnumerable<UIElement> GetAllChildren(DependencyObject parent)
        {
            if (parent != null)
            {
                var children = LogicalTreeHelper.GetChildren(parent);
                foreach (var child in children)
                    foreach (var grandChild in GetAllChildren((DependencyObject)child)) 
                        yield return grandChild;

                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
                {
                    // Retrieve child visual at specified index value.
                    var child = VisualTreeHelper.GetChild(parent, i) as Control;

                    if (child != null)
                    {
                        yield return child;

                        foreach (var grandChild in GetAllChildren(child))
                            yield return grandChild;
                    }
                }
            }
        }

    }

    #endregion

}

