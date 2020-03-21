using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using DAT10.Modules;
using DAT10Gui.Annotations;
using GalaSoft.MvvmLight.CommandWpf;

namespace DAT10Gui.View.Controls
{
    /// <summary>
    /// Interaction logic for OrderedSelectionList.xaml
    /// </summary>
    public sealed partial class OrderedSelectionList : UserControl, INotifyPropertyChanged
    {
        #region Dependency Properties
        public static readonly DependencyProperty CollectionHeaderProperty = 
            DependencyProperty.Register("CollectionHeader", typeof(string), typeof(OrderedSelectionList), new PropertyMetadata("Available"));

        [Bindable(true)]
        public string CollectionHeader
        {
            get { return (string) GetValue(CollectionHeaderProperty); }
            set { SetValue(CollectionHeaderProperty, value); }
        }

        public static readonly DependencyProperty SelectionHeaderProperty =
    DependencyProperty.Register("SelectionHeader", typeof(string), typeof(OrderedSelectionList), new PropertyMetadata("Selected"));

        [Bindable(true)]
        public string SelectionHeader
        {
            get { return (string)GetValue(SelectionHeaderProperty); }
            set { SetValue(SelectionHeaderProperty, value); }
        }

        public static readonly DependencyProperty CollectionProperty =
    DependencyProperty.Register("Collection", typeof(ObservableCollection<IModule>), typeof(OrderedSelectionList), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        [Bindable(true)]
        public ObservableCollection<IModule> Collection
        {
            get { return (ObservableCollection<IModule>) GetValue(CollectionProperty); }
            set { SetValue(CollectionProperty, value); }
        }

        public static readonly DependencyProperty SelectionProperty =
    DependencyProperty.Register("Selection", typeof(ObservableCollection<IModule>), typeof(OrderedSelectionList), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        [Bindable(true)]
        public ObservableCollection<IModule> Selection
        {
            get { return (ObservableCollection<IModule>) GetValue(SelectionProperty); }
            set { SetValue(SelectionProperty, value); }
        }

        public static readonly DependencyProperty OrderingProperty =
DependencyProperty.Register("Ordering", typeof(List<IDependent>), typeof(OrderedSelectionList), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        //private int _maxSelectedElements = -1;

        [Bindable(true)]
        public List<IDependent> Ordering
        {
            get { return (List<IDependent>)GetValue(OrderingProperty); }
            set { SetValue(OrderingProperty, value); }
        }

        public static readonly DependencyProperty MaxSelectedElementsProperty =
            DependencyProperty.Register("MaxSelectedElements", typeof(int), typeof(OrderedSelectionList), new PropertyMetadata(-1));

        [Bindable(true)]
        public int MaxSelectedElements
        {
            get { return (int) GetValue(MaxSelectedElementsProperty); }
            set { SetValue(MaxSelectedElementsProperty, value); }
        }

        public string Errors { get; private set; }
        public bool ShowErrors => !string.IsNullOrEmpty(Errors);

        public bool CanSelectMore => MaxSelectedElements == -1 || Selection.Count < MaxSelectedElements;
        #endregion

        #region Commands

        public RelayCommand<IModule> SelectModule { get; }
        public RelayCommand<IModule> RemoveModule { get; }

        public RelayCommand<IModule> MoveModuleUp { get; }
        public RelayCommand<IModule> MoveModuleDown { get; }

        #endregion

        public OrderedSelectionList()
        {
            InitializeComponent();

            SelectModule = new RelayCommand<IModule>(OnSelectModule);
            RemoveModule = new RelayCommand<IModule>(OnRemoveModule);
            MoveModuleUp = new RelayCommand<IModule>(OnMoveModuleUp);
            MoveModuleDown = new RelayCommand<IModule>(OnMoveModuleDown);

            CheckOrdering();

            OnPropertyChanged(nameof(CanSelectMore));
        }

        private void OnSelectModule(IModule moduleBase)
        {
            Collection.Remove(moduleBase);
            Selection.Add(moduleBase);

            CheckOrdering();
            OnPropertyChanged(nameof(CanSelectMore));
        }

        private void OnRemoveModule(IModule moduleBase)
        {
            Selection.Remove(moduleBase);
            Collection.Add(moduleBase);

            CheckOrdering();
            OnPropertyChanged(nameof(CanSelectMore));
        }

        private void CheckOrdering()
        {
            if (Ordering == null)
            {
                Errors = string.Empty;
                OnPropertyChanged(nameof(Errors));
                OnPropertyChanged(nameof(ShowErrors));

                return;
            }

            // Get index of all selections in ordering
            var indices = Selection.Select(s => Ordering.IndexOf((IDependent)s)).ToList();
            List<string> errors = new List<string>();

            short dependencies = 0;

            for (var i = 0; i < Selection.Count; i++)
            {
                var mod = (IDependent) Selection[i];

                dependencies |= mod.Affects;

                for (int j = 0; j < Selection.Count; j++)
                {
                    if (j > i && indices[j] != -1 && mod.IsDependentOn(Ordering[indices[j]]) && i != j)
                    {
                        errors.Add($"'{((IModule)mod).Name}' should occur after '{Selection[j].Name}'");
                    }
                }
            }

            // Dummy module to access AllDependencies and DependenciesToStrings properties
            var dummyModule = Ordering.Count > 0 ? Ordering[0] : (IDependent)Collection[0];

            if ((dependencies ^ dummyModule.AllDependencies) != 0)
            {
                errors.Add($"Missing modules for: {string.Join(", ", dummyModule.DependenciesToStrings(dependencies))}");
            }

            Errors = string.Join("\n", errors);
            OnPropertyChanged(nameof(Errors));
            OnPropertyChanged(nameof(ShowErrors));
        }

        private void OnMoveModuleUp(IModule moduleBase)
        {
            var i = Selection.IndexOf(moduleBase);

            if (i == 0)
                return;

            var temp = Selection[i - 1];
            Selection[i-1] = moduleBase;
            Selection[i] = temp;

            CheckOrdering();
        }

        private void OnMoveModuleDown(IModule moduleBase)
        {
            var i = Selection.IndexOf(moduleBase);

            if (i == Selection.Count-1)
                return;

            var temp = Selection[i + 1];
            Selection[i + 1] = moduleBase;
            Selection[i] = temp;
            CheckOrdering();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
