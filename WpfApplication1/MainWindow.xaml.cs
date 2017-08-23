using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
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

namespace WpfApplication1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.RichTextBox.AppendText("testing");

            Task.Run(() =>
            {

                var r = new TextRange(RichTextBox.Document.ContentStart, RichTextBox.Document.ContentEnd);
                Debug.Print($"{r.Text}, {Thread.CurrentThread.IsBackground}");
            });
        }
    }
}
