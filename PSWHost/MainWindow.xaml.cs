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
using System.Windows.Navigation;
using System.Windows.Shapes;

// PSWHost - Copyright (KeithGa@KeithGa.com) all rights reserved.
// Apache License 2.0

namespace PSWHost
{
    using System.Diagnostics;
    using System.Management.Automation;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly static TraceSource MyTracer = new TraceSource("PSWHost.MainWindow");

        public MainWindow()
        {

#if DEBUG
            MyTracer.Switch.Level = SourceLevels.All;
#endif
            MyTracer.TraceInformation("MainWindow Initialization...");
            InitializeComponent();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            MyTracer.TraceInformation("Main Window Closing...");
            if (PowershellHostControl1.isPowerShellRunning)
            {
                MyTracer.TraceInformation("Powershell still running, prompt the user for cancel confirmation.");
                e.Cancel = true;
                if (MessageBox.Show(PowerShell.Strings.CanClose, this.Title, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    MyTracer.TraceInformation("User selected to confirm close, force any in-progress powershell script to stop.");
                    PowershellHostControl1.Stop();
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            MyTracer.TraceInformation("MainWindow Loaded...");
            PowershellHostControl1.LoadAndRunPS1FromResource();
        }

        private void PSWControl_InvocationStateChanged(object sender, System.Management.Automation.PSInvocationStateChangedEventArgs e)
        {
            MyTracer.TraceInformation("MainWindow PowerShell State Changed ...");

            switch (e.InvocationStateInfo.State)
            {
                case PSInvocationState.Stopping:
                case PSInvocationState.NotStarted:
                case PSInvocationState.Running:
                default:
                    break;

                case PSInvocationState.Failed:

                    MyTracer.TraceInformation("InvocationStateChanged -> Failed");
                    Environment.ExitCode = 1;
                    break;

                case PSInvocationState.Completed:
                case PSInvocationState.Stopped:

                    MyTracer.TraceInformation("InvocationStateChanged -> Finished Successfully!");
                    Environment.ExitCode = PowershellHostControl1.ExitCode;

#if DEBUG
                    MyTracer.TraceInformation("InvocationStateChanged -> Skip final Close...!");
                    Application.Current.MainWindow.Title += PowerShell.Strings.Finished;
#else
                    this.Close();
#endif

                    break;
            }


        }

    }
}
