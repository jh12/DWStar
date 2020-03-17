using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using DAT10Gui.Annotations;
using DAT10Gui.View.Controls.CommonModelViewer;

namespace DAT10Gui.View.Controls.StarModelViewer
{
    /// <summary>
    /// Interaction logic for StarModelViewer.xaml
    /// </summary>
    public partial class StarModelViewer : UserControl
    {
        #region Dependency properties
        public static readonly DependencyProperty GraphProperty =
            DependencyProperty.Register("Graph", typeof(StarGraph), typeof(StarModelViewer), new PropertyMetadata(null));

        [Bindable(true)]
        public StarGraph Graph
        {
            get { return (StarGraph)GetValue(GraphProperty); }
            set { SetValue(GraphProperty, value); }
        }


        #endregion

        public StarModelViewer()
        {
            InitializeComponent();
            DataContext = this;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
