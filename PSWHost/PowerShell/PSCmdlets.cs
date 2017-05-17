using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// PSWHost - Copyright (KeithGa@KeithGa.com) all rights reserved.
// Apache License 2.0

namespace PSWHost
{
    using System.Management.Automation;
    using System.Data;
    using System.Collections;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Windows.Threading;
    using System.Windows.Markup;

    public enum OutputModeOption
    {
        None,
        Single,
        Multiple
    }

    static public class HostRoutines
    {
        /// <summary>
        /// When running within the PSWHost, $Host.PrivateData will contain a pointer back to our host control.
        /// </summary>
        public static PSWControl GetPSWControl(PSCmdlet MyCmdlet)
        {
            return (PSWControl)MyCmdlet.Host.PrivateData.BaseObject;
        }
    }

    /// <summary>
    /// This is an override of the out-GridView Cmdlet present in PowerShell
    /// instead of displaying the output in a DataGrid window, it will display on the PowerShell host WPF Window.
    /// </summary>
    [Cmdlet(VerbsCommon.Show, "HostGridView", DefaultParameterSetName = "PassThru", HelpUri = "http://go.microsoft.com/fwlink/?LinkID=113364")]
    public class HostGridView : PSCmdlet
    {

        private readonly static TraceSource MyTracer = new TraceSource("PSWControl.HostGridView");

        List<PSObject> FullList = null;

        [Parameter(ValueFromPipeline = true)]
        public PSObject InputObject
        { get; set; }

        [Parameter, ValidateNotNullOrEmpty]
        public string Title
        { get; set; }

        [Parameter(ParameterSetName = "Wait")]
        public SwitchParameter Wait
        { get; set; }

        [Parameter(ParameterSetName = "OutputMode")]
        public OutputModeOption OutputMode
        { get; set; }

        [Parameter()]
        public SwitchParameter PassThru
        {
            get
            {
                if (this.OutputMode != OutputModeOption.Multiple)
                    return new SwitchParameter(false);
                return new SwitchParameter(true);
            }
            set
            {
                this.OutputMode = (value.IsPresent ? OutputModeOption.Multiple : OutputModeOption.None);
            }
        }

        protected override void BeginProcessing()
        {
            FullList = new List<PSObject>();
            base.BeginProcessing();
        }

        protected override void ProcessRecord()
        {
            if (this.InputObject == null)
                return;

            // Sigh... Not all types/classes are processed in the same way with respect to pipeline input and 
            // specifying the data directly using the -InputObject parameter. So we need to process here.

            if (this.InputObject.BaseObject.GetType().IsArray)
            {
                foreach (object element in (object[])this.InputObject.BaseObject)
                    FullList.Add(new PSObject(element));
            }
            else if (this.InputObject.BaseObject is IDictionary)
            {
                foreach (object element in (IDictionary)this.InputObject.BaseObject)
                    FullList.Add(new PSObject(element));
            }
            else
            {
                FullList.Add(this.InputObject);
            }
            base.ProcessRecord();
        }

        protected override void EndProcessing()
        {
            bool isScalar = false;
            DataTable SendObj = null;
            List<System.Management.Automation.TableControlColumn> TableColl = null;

            base.EndProcessing();

            if (FullList.Count == 0)
                return; //nothing to do.

            ////////////////////////////////////////////////////////////////
            // Generate objects for call

            if (ScalarTypes.Contains(this.FullList[0].TypeNames[0]))
            {
                // The input is just a simple Scalar type, no need to do any extra processing.
                // Create a single colum, and slam the data into a DataTable

                MyTracer.TraceInformation("Sclar Types {0}", this.FullList[0].TypeNames[0]);

                isScalar = true;
                DataTable ArrayList = new DataTable();
                ArrayList.Columns.Add("Data", Nullable.GetUnderlyingType(this.FullList[0].BaseObject.GetType()) ?? this.FullList[0].BaseObject.GetType());
                foreach (PSObject Item in this.FullList)
                    ArrayList.Rows.Add(new object[] { Item.BaseObject });

                HostRoutines.GetPSWControl(this).Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => HostRoutines.GetPSWControl(this).FinishedGridView(
                    ArrayList.DefaultView, this.Wait || this.OutputMode != OutputModeOption.None, this.OutputMode == OutputModeOption.Multiple, !isScalar)));

            }
            else if ( HasObjectSpecialForammting(this.FullList[0], out SendObj, out TableColl))
            {
                // This class/type has some special formatting defined, so let's use PowerShell to format this table.

                MyTracer.TraceInformation("Check for special formatting");
                foreach (PSObject Item in this.FullList)
                {
                    SessionState.PSVariable.Set("MyOutGridViewDataItem", Item);
                    object[] values = new object[SendObj.Columns.Count];
                    for (int i = 0; i < TableColl.Count; i++)
                    {
                        if (TableColl[i].DisplayEntry.ValueType == DisplayEntryValueType.Property)
                        {
                            if (Item.Properties[TableColl[i].DisplayEntry.Value] != null)
                                values[i] = Item.Properties[TableColl[i].DisplayEntry.Value].Value;
                        }
                        else
                        {
                            Collection<PSObject> results = base.InvokeCommand.InvokeScript("$MyOutGridViewDataItem | foreach-object -process { " + TableColl[i].DisplayEntry.Value + " }");
                            if (results.Count > 0)
                                values[i] = results.First();
                        }
                    }

                    SendObj.Rows.Add(values);
                }

                HostRoutines.GetPSWControl(this).Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => HostRoutines.GetPSWControl(this).FinishedGridView(
                    SendObj.DefaultView, this.Wait || this.OutputMode != OutputModeOption.None, this.OutputMode == OutputModeOption.Multiple, !isScalar)));

            }
            else
            {
                // this non-scalar type/class does not have any special formatting, so just slam into an ArrayList.
                MyTracer.TraceInformation("Some other processing... ");

                ArrayList sendList = new ArrayList();
                sendList.AddRange(FullList); 

                HostRoutines.GetPSWControl(this).Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => HostRoutines.GetPSWControl(this).FinishedGridView(
                    sendList, this.Wait || this.OutputMode != OutputModeOption.None, this.OutputMode == OutputModeOption.Multiple, !isScalar)));

            }


            if (!this.Wait && this.OutputMode == OutputModeOption.None)
                return; // Done do not wait for output...

            PSObject result = null;
            if (HostRoutines.GetPSWControl(this).WaitForUserNavigationEvent(out result))
            {
                // Result from the GridView will be an array of indexes. Use this to return objects.
                List<int> MySelectedIndex = (List<int>)result.BaseObject;
                MyTracer.TraceInformation("Out-GridView() Complete Count: {0}", MySelectedIndex.Count);

                foreach (int Row in MySelectedIndex)
                    if (Row >= 0 && Row < this.FullList.Count)
                        base.WriteObject(this.FullList[Row]);
            }

            // Cleanup
            base.StopProcessing();
            this.FullList = null;
        }


        private bool HasObjectSpecialForammting(PSObject Sample, out DataTable NewTable, out List<System.Management.Automation.TableControlColumn> TableColl)
        {
            // Use Get-FormatData to determine if this class/type has any pre-defined formatting defined, and if so load that formatting.

            NewTable = null;
            TableColl = null;
            foreach (PSObject result in base.InvokeCommand.InvokeScript("Get-FormatData -TypeName '" + Sample.TypeNames[0] + "'"))
            {
                if (result != null && result.BaseObject.GetType() == typeof(System.Management.Automation.ExtendedTypeDefinition))
                {
                    foreach (var child in ((System.Management.Automation.ExtendedTypeDefinition)result.BaseObject).FormatViewDefinition)
                    {
                        if (child.Control.GetType() == typeof(System.Management.Automation.TableControl))
                        {
                            NewTable = new DataTable();
                            TableColl = ((System.Management.Automation.TableControl)child.Control).Rows[0].Columns;
                            for (int i = 0; i < ((System.Management.Automation.TableControl)child.Control).Headers.Count && i < ((System.Management.Automation.TableControl)child.Control).Rows[0].Columns.Count; i++)
                            {
                                if (!string.IsNullOrEmpty(((System.Management.Automation.TableControl)child.Control).Headers[i].Label))
                                    ((DataTable)NewTable).Columns.Add(((System.Management.Automation.TableControl)child.Control).Headers[i].Label);
                                else if (!string.IsNullOrEmpty(((System.Management.Automation.TableControl)child.Control).Rows[0].Columns[i].DisplayEntry.Value))
                                    ((DataTable)NewTable).Columns.Add(((System.Management.Automation.TableControl)child.Control).Rows[0].Columns[i].DisplayEntry.Value);
                            }
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private List<string> ScalarTypes = new List<string>() { "System.String", "System.SByte", "System.Byte", "System.Int16", "System.UInt16",
            "System.Int32", "System.UInt32", "System.Int64", "System.UInt64", "System.Char", "System.Single", "System.Double", "System.Boolean",
            "System.Decimal", "System.IntPtr", "System.Security.SecureString", "System.Numerics.BigInteger" };

    }

    /// <summary>
    /// Display a set of Key Value Pairs and allow the user to edit the values in a DataGrid window.
    /// </summary>
    [Cmdlet(VerbsData.Edit, "KeyValuePair", HelpUri = "http://go.microsoft.com/fwlink/?LinkID=113364")]
    public class HostGridEdit : PSCmdlet
    {
        private readonly static TraceSource MyTracer = new TraceSource("PSWControl.KeyValuePair");

        List<PSObject> FullList = null;

        [Parameter(ValueFromPipeline = true)]
        public PSObject InputObject
        { get; set; }

        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            FullList = new List<PSObject>();
        }

        [Parameter]
        public int[] HeaderWidths
        { get; set; }


        protected override void ProcessRecord()
        {
            if (this.InputObject == null)
                return;

            if (this.InputObject.BaseObject.GetType().IsArray)
            {
                foreach (object element in (object[])this.InputObject.BaseObject)
                    FullList.Add(new PSObject(element));
            }
            else if (this.InputObject.BaseObject is IDictionary)
            {
                foreach (object element in (IDictionary)this.InputObject.BaseObject)
                    FullList.Add(new PSObject(element));
            }
            else
            {
                FullList.Add(this.InputObject);
            }
            base.ProcessRecord();
        }

        protected override void EndProcessing()
        {
            ArrayList SendObj = new ArrayList();

            if (FullList.Count == 0)
                return; //nothing to do.

            SendObj = new ArrayList();
            ((ArrayList)SendObj).AddRange(FullList);

            ////////////////////////////////////////////////////////////////

            // Display List
            HostRoutines.GetPSWControl(this).Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => HostRoutines.GetPSWControl(this).FinishedGridEdit(
                SendObj, this.HeaderWidths)));

            PSObject result = null;
            if (HostRoutines.GetPSWControl(this).WaitForUserNavigationEvent(out result))
            {
                List<PSObject> ResultObjects = (List<PSObject>)result.BaseObject;

                Trace.TraceInformation("Out-GridEdit() Complete Count: {0}", ResultObjects.Count);

                foreach (var Item in ResultObjects)
                    base.WriteObject(Item);
            }

            // Cleanup
            base.StopProcessing();
            base.EndProcessing();

        }

    }

    ///////////////////////////////////////////////////////////////////


    [Cmdlet(VerbsCommon.Show, "XAMLControl")]
    public class ShowXAMLControl : PSCmdlet
    {
        private readonly static TraceSource MyTracer = new TraceSource("PSWControl.ShowXAMLWindow");

        /// <summary>
        /// XAML definition. Can be a <Window> or <UserControl>
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        public string XAMLString
        { get; set; }

        /// <summary>
        /// Key and Value pair of Control Names (keys), and Defaults(Values) to be loaded in the control.
        /// </summary>
        [Parameter(Mandatory = false)]
        public Hashtable DefaultValues
        { get; set; }

        /// <summary>
        /// Powershell Scripts used as event controls
        /// </summary>
        [Parameter(Mandatory = false)]
        public ScriptBlock ScriptBlock
        { get; set; }

        /// <summary>
        /// Behaviour change: THere is a control within the XAML defintion that should default for all new StdOut
        /// </summary>
        [Parameter(Mandatory = false)]
        public String NewStdOut
        { get; set; }

        /// <summary>
        /// Behaviour change: Wait for a user initiated event before exiting. 
        /// </summary>
        [Parameter(Mandatory = false)]
        public SwitchParameter WaitForUser
        { get; set; }

        /// <summary>
        /// Behaviour Change: Display the XML control as full screen.
        /// </summary>
        [Parameter(Mandatory = false)]
        public SwitchParameter FullScreen
        { get; set; }


        protected override void ProcessRecord()
        {
#if DEBUG
            MyTracer.Switch.Level = SourceLevels.All;
#endif
            MyTracer.TraceInformation("Show-XAMLWindow New ProcessRecord()");
            base.ProcessRecord();
            ErrorRecord Errors = null;

            System.Windows.UIElement newElement = null;
            try
            {
                ParserContext context = new ParserContext();

                // Some processing of the XAML string when deveoped using another Visual Studio instance.
                string ReplacementXAML = (new System.Text.RegularExpressions.Regex("(x\\:Class|Title)=\"[^\"]*\"")).Replace(XAMLString, "");
                string ReplacementXAML2 = ReplacementXAML.Replace("<Window ", "<UserControl ").Replace("</Window>", "</UserControl>");
                newElement = (System.Windows.UIElement)XamlReader.Parse(ReplacementXAML2, context);
                newElement = null; // We don't need the object, just need to parse it to see if there are any errors.
            }
            catch (Exception ex)
            {
                this.ThrowTerminatingError(new ErrorRecord(ex, "XAMLReader.Parse()", ErrorCategory.ParserError, this));
            }

            // Basic, just call the control with these variables...
            HostRoutines.GetPSWControl(this).Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                HostRoutines.GetPSWControl(this).DisplayXAMLControl(this)));

            MyTracer.TraceInformation(">>>>>>>>>>>>>>>>>>>>>>>>  Show-XAMLWindow Call done!()");
            if (Errors != null)
            {
                this.ThrowTerminatingError(Errors);
            }
            else // if ( PassThru.IsPresent )
            {
                // Then output the result variable to the pipeline
                PSObject result = null;
                if (HostRoutines.GetPSWControl(this).WaitForUserNavigationEvent(out result))
                {
                    if (result != null)
                        base.WriteObject(result);
                }
            }

        }

    }

    [Cmdlet(VerbsCommon.Remove,"XAMLControl")]
    public class RemoveXAMLControl : PSCmdlet
    {
        /// <summary>
        /// If a XAML window is display async
        /// </summary>

        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        public System.Windows.Controls.UserControl InputObject
        { get; set; }

        protected override void ProcessRecord()
        {
            base.ProcessRecord();
            if (InputObject.GetType().Name == "UserControl")
            {
                HostRoutines.GetPSWControl(this).Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                    HostRoutines.GetPSWControl(this).RemoveXAMLControl(this)));
            }
        }

        protected override void EndProcessing()
        {
            base.EndProcessing();
        }
    }

}
