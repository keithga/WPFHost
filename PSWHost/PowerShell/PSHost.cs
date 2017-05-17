using System;
using System.Collections.Generic;
using System.Linq;

// PSWHost - Copyright (KeithGa@KeithGa.com) all rights reserved.
// Apache License 2.0

namespace PSWHost
{

    // PowerShell Host Control
    // Mostly stolen from PS2Wiz.codeplex.com

    using System.Management.Automation;
    using System.Management.Automation.Host;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Threading;

    internal class MyHost : PSHost
    {

        private readonly static TraceSource MyTracer = new TraceSource("PSWHost");

        private PSWControl Parent = null;
        private MyHostUserInterface myHostUserInterface = null;
        private Guid myId = Guid.NewGuid();

        public MyHost(PSWControl _Parent)
        {
#if DEBUG
            MyTracer.Switch.Level = SourceLevels.All;
#endif
            Parent = _Parent;
            myHostUserInterface = new MyHostUserInterface(_Parent);
        }

        ///////////////////////////////////////////////////////////////////

        public override System.Globalization.CultureInfo CurrentCulture
        {
            get { return System.Threading.Thread.CurrentThread.CurrentCulture; }
        }

        public override System.Globalization.CultureInfo CurrentUICulture
        {
            get { return System.Threading.Thread.CurrentThread.CurrentUICulture; }
        }

        public override Guid InstanceId
        {
            get { return this.myId; }
        }

        public override string Name
        {
            get { return "PowerShellWPFHost"; }
        }

        public override PSHostUserInterface UI
        {
            get { return this.myHostUserInterface; }
        }

        public override Version Version
        {
            get { return new Version(1, 0, 2, 0); }
        }

        public override void EnterNestedPrompt()
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        public override void ExitNestedPrompt()
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        public override void NotifyBeginApplication()
        {
            return;
        }

        public override void NotifyEndApplication()
        {
            return;
        }

        public override void SetShouldExit(int exitCode)
        {
            MyTracer.TraceInformation("PowerShell script has finsished with ExitCode: {0}", exitCode);
            this.Parent.ExitCode = exitCode;
        }

        public override PSObject PrivateData
        {
            get
            {
                return new PSObject(this.Parent);
            }
        }

    }

    internal class MyHostUserInterface : PSHostUserInterface
    {

        private readonly static TraceSource MyTracer = new TraceSource("PSWHost");
        PSWControl Parent = null;
        private MyRawUserInterface myRawUi = null;

        public MyHostUserInterface(PSWControl _Parent)
        {
#if DEBUG
            MyTracer.Switch.Level = SourceLevels.All;
#endif
            Parent = _Parent;
            myRawUi = new MyRawUserInterface(Parent);
        }

        ///////////////////////////////////////////////////////////////////

        public override PSHostRawUserInterface RawUI
        {
            get { return this.myRawUi; }
        }

        public override Dictionary<string, PSObject> Prompt(string caption, string message, System.Collections.ObjectModel.Collection<FieldDescription> descriptions)
        {
            MyTracer.TraceInformation("Prompt(caption:{0},message:{1},Description count:{2})", caption, message, descriptions.Count);
            this.Parent.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => this.Parent.Prompt(caption, message, descriptions)));
            PSObject result = null;
            if (this.Parent.WaitForUserNavigationEvent(out result))
            {
                MyTracer.TraceInformation("Prompt() Complete Count: {0}", ((Dictionary<string, PSObject>)result.BaseObject).Count);
                return (Dictionary<string, PSObject>)result.BaseObject;
            }
            MyTracer.TraceInformation("Prompt() Empty");
            return null;
        }

        public override int PromptForChoice(string caption, string message, System.Collections.ObjectModel.Collection<ChoiceDescription> choices, int defaultChoice)
        {
            MyTracer.TraceInformation("PromptForChoice(caption:{0},message:{1})", caption, message);
            this.Parent.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => this.Parent.PromptForChoice(caption, message, choices, defaultChoice)));
            PSObject result = null;
            if (this.Parent.WaitForUserNavigationEvent(out result))
            {
                MyTracer.TraceInformation("PromptForChoice() Complete");
                return (int)result.BaseObject;
            }
            MyTracer.TraceInformation("PromptForChoice() Empty");
            return -1;
        }

        public override PSCredential PromptForCredential(string caption, string message, string userName, string targetName, PSCredentialTypes allowedCredentialTypes, PSCredentialUIOptions options)
        {
            MyTracer.TraceInformation("PromptForCredentialEx(caption:{0},message:{1})", caption, message);
            string Name = "Credential";
            FieldDescription DefaultCred = new FieldDescription(Name);
            if (!string.IsNullOrEmpty(userName))
            {
                DefaultCred.DefaultValue = new PSObject(userName);
            }
            DefaultCred.SetParameterType(typeof(PSCredential));

            var Prompts = new System.Collections.ObjectModel.Collection<FieldDescription>() { DefaultCred };
            var results = this.Prompt(caption, message, Prompts);

            if (results != null)
            {
                if (results.ContainsKey(Name))
                {
                    MyTracer.TraceInformation("PromptForCredential() Complete");
                    return (PSCredential)(results[Name].BaseObject);
                }
            }
            MyTracer.TraceInformation("PromptForCredential() Empty");
            return null;
        }

        public override PSCredential PromptForCredential(string caption, string message, string userName, string targetName)
        {
            MyTracer.TraceInformation("PromptForCredential(caption:{0},message:{1},userName:{2},TargetName:{3})", caption, message, userName, targetName);
            return this.PromptForCredential(caption, message, userName, targetName, PSCredentialTypes.Default, PSCredentialUIOptions.Default);
        }


        public override string ReadLine()
        {
            MyTracer.TraceInformation("ReadLine()");
            this.Parent.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => Parent.ShowReadLine()));
            PSObject result = null;
            if (this.Parent.WaitForUserNavigationEvent(out result))
            {
                MyTracer.TraceInformation("ReadLine() Complete");
                return (string)result.BaseObject;
            }
            MyTracer.TraceInformation("ReadLine() Empty");
            return "";
        }

        public override System.Security.SecureString ReadLineAsSecureString()
        {
            MyTracer.TraceInformation("ReadLineAsSecureString() Start");
            this.Parent.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => Parent.ShowPassword()));
            PSObject result = null;
            if (this.Parent.WaitForUserNavigationEvent(out result))
            {
                MyTracer.TraceInformation("ReadLineAsSecureString() Complete");
                return (System.Security.SecureString)result.BaseObject;
            }
            MyTracer.TraceInformation("ReadLineAsSecureString() Empty");
            return new System.Security.SecureString();
        }

        public override void WriteProgress(long sourceId, ProgressRecord record)
        {
            // WAY too verbose
            // MyTracer.TraceInformation("WriteProgress(Source:{0},Activity:{1},ParentActivity:{2})",sourceId,record.ActivityId,record.ParentActivityId);
            this.Parent.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => Parent.WriteProgress(sourceId, record)));
        }

        public override void Write(string value)
        {
            this.WriteExCommon(null, null, "StdOut ", value, false);
        }

        public override void Write(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value)
        {
            if (foregroundColor == ConsoleColor.Black && backgroundColor == ConsoleColor.Black)
            {
                // Special override for "CLS" scenario.
                this.Write(value);
            }
            else
            {
                this.WriteExCommon(strColor(foregroundColor), strColor(backgroundColor), "StdOut ", value, false);
            }
        }

        public override void WriteLine()
        {
            this.WriteLine("");
        }

        public override void WriteLine(string value)
        {
            this.WriteExCommon(null, null, "StdOut ", value, true);
        }

        public override void WriteLine(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value)
        {
            if (foregroundColor == ConsoleColor.Black && backgroundColor == ConsoleColor.Black)
            {
                // Special override for "CLS" scenario.
                this.WriteLine(value);
            }
            else
            {
                this.WriteExCommon(strColor(foregroundColor), strColor(backgroundColor), "StdOut ", value, true);
            }
        }

        public override void WriteDebugLine(string message)
        {
            this.WriteExCommon(strColor(ConsoleColor.DarkGray), "", "DEBUG  ", message, true);
        }

        public override void WriteErrorLine(string value)
        {
            this.WriteExCommon(strColor(ConsoleColor.DarkGray), "", "ERROR  ", value, true);
        }

        public override void WriteVerboseLine(string message)
        {
            this.WriteExCommon(strColor(ConsoleColor.DarkGray), "", "VERBOSE", message, true);
        }

        public override void WriteWarningLine(string message)
        {
            this.WriteExCommon(strColor(ConsoleColor.DarkGray), "", "WARNING", message, true);
        }

        public void WriteExCommon(string foregroundColor, string backgroundColor, string category, string value, bool newLine)
        {
            if (string.IsNullOrEmpty(value) && !newLine)
                return;

            MyTracer.TraceInformation("{0}: {1}", category, value);

            this.Parent.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => 
                Parent.WriteLabel(foregroundColor,backgroundColor,value,newLine)));
        }

        public string strColor(ConsoleColor newColor)
        {
            System.Drawing.Color MyColor = System.Drawing.Color.FromName(newColor.ToString());
            return System.Drawing.ColorTranslator.ToHtml(System.Drawing.Color.FromArgb(MyColor.ToArgb())).ToString();
        }


    }

    internal class MyRawUserInterface : PSHostRawUserInterface
    {
        private readonly static TraceSource MyTracer = new TraceSource("PSWHost");
        PSWControl Parent = null;

        public MyRawUserInterface(PSWControl _Parent)
        {
#if DEBUG
            MyTracer.Switch.Level = SourceLevels.All;
#endif
            Parent = _Parent;
        }

        ///////////////////////////////////////////////////////////////////

        /// <summary>
        /// Required only for "out-string"
        /// </summary>
        public override System.Management.Automation.Host.Size BufferSize
        {
            get { return new System.Management.Automation.Host.Size(120, 50); }
            set { throw new NotImplementedException("The method or operation is not implemented."); }
        }

        public override ConsoleColor BackgroundColor
        { get; set; }

        public override Coordinates CursorPosition
        { get; set; }

        public override int CursorSize
        { get; set; }

        public override ConsoleColor ForegroundColor
        { get; set; }

        public override bool KeyAvailable
        { get { return false; } }

        public override System.Management.Automation.Host.Size MaxPhysicalWindowSize
        { get { return MaxPhysicalWindowSize; } }

        public override System.Management.Automation.Host.Size MaxWindowSize
        { get { return MaxPhysicalWindowSize; } }

        public override Coordinates WindowPosition
        { get; set; }

        public override System.Management.Automation.Host.Size WindowSize
        { get; set; }

        public override string WindowTitle
        {
            get { return Console.Title; }
            set
            {
                MyTracer.TraceInformation("WindowTitle({0})", value);
                this.Parent.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => Application.Current.MainWindow.Title = value)); 
            }
        }

        public override void FlushInputBuffer()
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        public override BufferCell[,] GetBufferContents(Rectangle rectangle)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        public override KeyInfo ReadKey(ReadKeyOptions options)
        {
            MyTracer.TraceInformation("ReadKey({0})", options.ToString());
            this.Parent.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => this.Parent.ReadKey(options)));
            PSObject result = null;
            if (this.Parent.WaitForUserNavigationEvent(out result))
            {
                return (KeyInfo)result.BaseObject;
            }
            return new KeyInfo();
        }

        public override void ScrollBufferContents(Rectangle source, Coordinates destination, Rectangle clip, BufferCell fill)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        public override void SetBufferContents(Coordinates origin, BufferCell[,] contents)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        public override void SetBufferContents(Rectangle rectangle, BufferCell fill)
        {
            if (fill.BufferCellType == BufferCellType.Complete && fill.Character == ' ' && rectangle == (new Rectangle(-1, -1, -1, -1)))
            {
                MyTracer.TraceInformation("SetBufferContents-->ClearScreen");
                this.Parent.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => this.Parent.ClearScreen()));
            }
            else
            {
                throw new NotImplementedException("The method or operation is not implemented.");
            }
        }

    }

}

