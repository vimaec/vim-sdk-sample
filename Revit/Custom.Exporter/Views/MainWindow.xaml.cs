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
using Custom.Exporter.ViewModels;

namespace Custom.Exporter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindowViewModel ViewModel { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="viewModel"></param>
        public MainWindow(MainWindowViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            DataContext = ViewModel;
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SaveAndReset()
        {
            ViewModel.Save();
            ViewModel.Reset();
        }

        /// <summary>
        /// Invoked by the base Window when it will be closed using the Operating System window manager.
        /// </summary>
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            SaveAndReset();

            // NOTE: We need to hide the window if we're in Revit. Destroying it prevents us from re-opening properly from the ribbon afterwards.
            e.Cancel = true;
            Hide();
        }
    }
}
