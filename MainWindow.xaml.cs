using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using WhiteScan.ViewModels;

namespace WhiteScan
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow(MainViewModel viewModel)
        {
            System.Diagnostics.Debug.WriteLine("🏗️ MainWindow constructor called");
            System.Diagnostics.Debug.WriteLine("📋 Setting DataContext...");
            
            InitializeComponent();
            DataContext = viewModel;
            
            System.Diagnostics.Debug.WriteLine("✅ MainWindow DataContext set successfully");
            System.Diagnostics.Debug.WriteLine("🎯 MainWindow ready for use");
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void DataGrid_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.ClearOldResults();
            }
        }
    }
}