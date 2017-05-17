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

// CommandLink - Custom License
// From https://github.com/aybe/Windows-API-Code-Pack-1.1

namespace PSWHost
{

    using System.ComponentModel;
    using System.Windows.Markup;

    /// <summary>
    /// Interaction logic for CommandLink.xaml
    /// </summary>
    public partial class CommandLink : UserControl, INotifyPropertyChanged, IComponentConnector
    {
        private string link;
        private string note;
        private ImageSource icon;

        public event RoutedEventHandler Click;
        public event PropertyChangedEventHandler PropertyChanged;

        public RoutedUICommand Command
        {
            get;
            set;
        }

        public string Link
        {
            get
            {
                return this.link;
            }
            set
            {
                this.link = value;

                SetHeight();

                if (this.PropertyChanged != null)
                {
                    this.PropertyChanged(this, new PropertyChangedEventArgs("Link"));
                }
            }
        }

        public void SetHeight()
        {
            if (string.IsNullOrEmpty(this.note) && (this.icon == null))
                this.Height = 30;
            else if (string.IsNullOrEmpty(this.note))
                this.Height = 35;
            else
                this.Height = 45;
        }

        public string Note
        {
            get
            {
                return this.note;
            }
            set
            {
                this.note = value;

                SetHeight();

                if (this.PropertyChanged != null)
                {
                    this.PropertyChanged(this, new PropertyChangedEventArgs("Note"));
                }
            }
        }

        public ImageSource Icon
        {
            get
            {
                return this.icon;
            }
            set
            {
                this.icon = value;

                SetHeight();

                if (this.PropertyChanged != null)
                {
                    this.PropertyChanged(this, new PropertyChangedEventArgs("Icon"));
                }
            }
        }

        public bool? IsCheck
        {
            get
            {
                return this.button.IsChecked;
            }
            set
            {
                this.button.IsChecked = value;
            }
        }

        public static bool IsPlatformSupported
        {
            get
            {
                return true;
            }
        }

        public CommandLink()
        {
            base.DataContext = this;
            this.InitializeComponent();
            this.button.Click += new RoutedEventHandler(this.button_Click);

            SetHeight();

        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            e.Source = this;
            if (this.Click != null)
            {
                this.Click(sender, e);
            }
        }

    }

}
