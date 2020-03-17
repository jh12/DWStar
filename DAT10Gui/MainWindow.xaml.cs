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
using DAT10Gui.ViewModel;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Messaging;
using MahApps.Metro.Controls;

namespace DAT10Gui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            var ctx = ((MainViewModel)DataContext);

            Title = $"DAT 10 - {ctx.MenuItems[ctx.SelectedIndex].Label}";
        }

        private void HamburgerMenu_OnItemClick(object sender, ItemClickEventArgs e)
        {
            var clickedItem = e.ClickedItem as HamburgerMenuItem;

            //var instance = SimpleIoc.Default.GetInstance(clickedItem.TargetPageType);

            Title = $"DAT 10 - {clickedItem.Label}";

            Messenger.Default.Send(clickedItem.TargetPageType, "SwitchHamburgerContext");
        }
    }
}
