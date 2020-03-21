using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using DAT10.Core;
using DAT10.Core.Setting;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;

namespace DAT10Gui.ViewModel
{
    public class ConnectionViewModel : ViewModelBase
    {
        private readonly ModuleEngine _moduleEngine;

        public ObservableCollection<string> SourceTypes { get; set; }
        public ObservableCollection<ConnectionInfo> Connections { get; set; }

        public RelayCommand<ConnectionInfo> CreateConnection { get; set; }
        public RelayCommand<ConnectionInfo> RemoveConnection { get; set; }

        public ConnectionViewModel(ModuleEngine moduleEngine)
        {
            _moduleEngine = moduleEngine;

            SourceTypes = new ObservableCollection<string>(_moduleEngine.SchemaInferenceModules.Select(m => m.SupportedSourceType()).Distinct().ToList());

            Connections = new ObservableCollection<ConnectionInfo>(moduleEngine.Connections);

            CreateConnection = new RelayCommand<ConnectionInfo>(OnCreateConnection);
            RemoveConnection = new RelayCommand<ConnectionInfo>(OnRemoveConnection);

            Connections.CollectionChanged += ConnectionsOnCollectionChanged;
        }

        private void ConnectionsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
        {
            _moduleEngine.Connections = new List<ConnectionInfo>(Connections);
        }

        private void OnCreateConnection(ConnectionInfo connectionInfo)
        {
            Connections.Add(connectionInfo);
            RaisePropertyChanged(nameof(Connections));
        }

        private void OnRemoveConnection(ConnectionInfo connectionInfo)
        {
            Connections.Remove(connectionInfo);
            RaisePropertyChanged(nameof(Connections));
        }
    }
}
