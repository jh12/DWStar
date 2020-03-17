using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DAT10Gui.View.Controls;
using DAT10Gui.ViewModel;
using MahApps.Metro.Controls;
using MaterialDesignThemes.Wpf;

namespace DAT10Gui.View
{
    /// <summary>
    /// Interaction logic for TemplateView.xaml
    /// </summary>
    public partial class ConfigurationView : UserControl
    {
        public ConfigurationView()
        {
            InitializeComponent();
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scv = (ScrollViewer) sender;
            scv.ScrollToHorizontalOffset(scv.HorizontalOffset - e.Delta/3f);
            e.Handled = true;
        }

        private void OnConfigurationCategoryClick(object sender, RoutedEventArgs e)
        {
            ConfigurationCategory category = (ConfigurationCategory) ((Button) sender).DataContext;

            ConfigurationDialogContentControl.Content = category;

            ConfigurationDialogContent.Width = ActualWidth - 50;
            ConfigurationDialogContent.Height = Application.Current.MainWindow.ActualHeight - 50;

            DialogHost.Show(ConfigurationDialogContent, "RootDialog");
        }
    }
}
