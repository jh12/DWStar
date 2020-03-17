using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using DAT10Gui.Annotations;

namespace DAT10Gui.View.Controls.CommonModelViewer
{
    /// <summary>
    /// Interaction logic for CommonModelViever.xaml
    /// </summary>
    public partial class CommonModelViewer : UserControl, INotifyPropertyChanged
    {
        #region Dependency properties
        public static readonly DependencyProperty GraphProperty =
            DependencyProperty.Register("Graph", typeof(CommonGraph), typeof(CommonModelViewer), new PropertyMetadata(null));

        [Bindable(true)]
        public CommonGraph Graph
        {
            get { return (CommonGraph)GetValue(GraphProperty); }
            set { SetValue(GraphProperty, value); }
        }


        #endregion

        public CommonModelViewer()
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
