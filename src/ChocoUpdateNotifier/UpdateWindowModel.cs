using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ChocoUpdateNotifier
{
    public class UpdateWindowModel : DependencyObject
    {
        public event EventHandler IsSelectedChanged;

        /// <summary>
        /// Gets or sets whether the view is loading
        /// </summary>
        public bool IsLoading
        {
            get { return (bool)GetValue(IsLoadingProperty); }
            set { SetValue(IsLoadingProperty, value); }
        }
        public static readonly DependencyProperty IsLoadingProperty =
            DependencyProperty.Register("IsLoading", typeof(bool), typeof(UpdateWindowModel),
                new PropertyMetadata(false, new PropertyChangedCallback(OnIsLoadingChanged)));

        private static void OnIsLoadingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UpdateWindowModel model)
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }


        /// <summary>
        /// Gets or sets a list of packages
        /// </summary>
        public ObservableCollection<PackageModel> Packages { get; set; } = new ObservableCollection<PackageModel>();

        /// <summary>
        /// Gets or sets the command to update the selected packages
        /// </summary>
        public ICommand UpdateSelectedCommand { get; set; }

        /// <summary>
        /// Gets or sets the command to update the selected packages
        /// </summary>
        public ICommand UpdateAllCommand { get; set; }

        private bool _updating;

        public UpdateWindowModel()
        {
            UpdateSelectedCommand = new RelayCommand<object>(UpdateSelectedPackages, _ => !IsLoading && !_updating);
            UpdateAllCommand = new RelayCommand<object>(UpdateAllPackages, _ => !IsLoading && !_updating);

            IsLoading = true;
            Task.Run(LoadOutdatedChocoPackages);
        }

        /// <summary>
        /// Loads all outdated choco packages
        /// </summary>
        private void LoadOutdatedChocoPackages()
        {
            var pcks = Choco.OutdatedPackages(false);
            Dispatcher.BeginInvoke(new Action(() =>
            {
                IsLoading = false;
                foreach (var pck in pcks)
                {
                    var model = new PackageModel(pck.Name, pck.OldVersion, pck.NewVersion, pck.IsPinned);
                    model.IsSelectedChanged += IsSelectedChanged;
                    Packages.Add(model);
                }
            }));
        }

        private void UpdateSelectedPackages(object _)
        {
            // Disable button
            _updating = true;
            CommandManager.InvalidateRequerySuggested();

            Choco.UpdatePackages(Packages.Where(pck => pck.IsSelected).Select(pck => pck.Name).ToArray());

            // End app
            Environment.Exit(0);
        }

        private void UpdateAllPackages(object _)
        {
            // Disable button
            _updating = true;
            CommandManager.InvalidateRequerySuggested();

            Choco.UpdateAllPackages();

            // End app
            Environment.Exit(0);
        }

        public class PackageModel : DependencyObject
        {
            public event EventHandler IsSelectedChanged;

            /// <summary>
            /// Gets or sets the name
            /// </summary>
            public string Name
            {
                get { return (string)GetValue(NameProperty); }
                set { SetValue(NameProperty, value); }
            }
            public static readonly DependencyProperty NameProperty =
                DependencyProperty.Register("Name", typeof(string), typeof(PackageModel), new PropertyMetadata("Package"));

            /// <summary>
            /// Gets or sets whether the package is selected
            /// </summary>
            public bool IsSelected
            {
                get { return (bool)GetValue(IsSelectedProperty); }
                set { SetValue(IsSelectedProperty, value); }
            }
            public static readonly DependencyProperty IsSelectedProperty =
                DependencyProperty.Register("IsSelected", typeof(bool), typeof(PackageModel),
                    new PropertyMetadata(false, new PropertyChangedCallback(OnIsSelectedChanged)));

            private static void OnIsSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            {
                if (d is PackageModel model)
                {
                    model.IsSelectedChanged?.Invoke(model, null);
                }
            }

            /// <summary>
            /// Gets or sets wheter the package is pinned
            /// </summary>
            public bool IsPinned
            {
                get { return (bool)GetValue(IsPinnedProperty); }
                set { SetValue(IsPinnedProperty, value); }
            }
            public static readonly DependencyProperty IsPinnedProperty =
                DependencyProperty.Register("IsPinned", typeof(bool), typeof(PackageModel), new PropertyMetadata(false));

            /// <summary>
            /// Gets or sets the old version
            /// </summary>
            public string OldVersion
            {
                get { return (string)GetValue(OldVersionProperty); }
                set { SetValue(OldVersionProperty, value); }
            }
            public static readonly DependencyProperty OldVersionProperty =
                DependencyProperty.Register("OldVersion", typeof(string), typeof(PackageModel), new PropertyMetadata("?"));

            /// <summary>
            /// Gets or sets the new version
            /// </summary>
            public string NewVersion
            {
                get { return (string)GetValue(NewVersionProperty); }
                set { SetValue(NewVersionProperty, value); }
            }
            public static readonly DependencyProperty NewVersionProperty =
                DependencyProperty.Register("NewVersion", typeof(string), typeof(PackageModel), new PropertyMetadata("?"));


            public PackageModel(string Name, string OldVersion, string NewVersion, bool IsPinned)
            {
                this.Name = Name;
                this.OldVersion = OldVersion;
                this.NewVersion = NewVersion;
                this.IsPinned = IsPinned;
            }
        }
    }
}
