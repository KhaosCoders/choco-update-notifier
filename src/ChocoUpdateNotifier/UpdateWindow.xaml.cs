using MahApps.Metro.Controls;

namespace ChocoUpdateNotifier
{
    /// <summary>
    /// Interaction logic for UpdateWindow.xaml
    /// </summary>
    public partial class UpdateWindow : MetroWindow
    {
        public UpdateWindow()
        {
            var model = new UpdateWindowModel();
            model.IsSelectedChanged += Model_IsSelectedChanged;
            DataContext = model;
            InitializeComponent();
        }

        private bool _lockSelection;

        private void Model_IsSelectedChanged(object sender, System.EventArgs e)
        {
            if (!_lockSelection && sender is UpdateWindowModel.PackageModel model)
            {
                if (model.IsSelected && !this.lviPackages.SelectedItems.Contains(model))
                {
                    this.lviPackages.SelectedItems.Add(model);
                }
                else if(!model.IsSelected && this.lviPackages.SelectedItems.Contains(model))
                {
                    this.lviPackages.SelectedItems.Remove(model);
                }
            }
        }

        private void lviPackages_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            _lockSelection = true;
            if (e.AddedItems != null)
            {
                foreach(UpdateWindowModel.PackageModel pck in e.AddedItems)
                {
                    pck.IsSelected = true;
                }
            }
            if (e.RemovedItems != null)
            {
                foreach (UpdateWindowModel.PackageModel pck in e.RemovedItems)
                {
                    pck.IsSelected = false;
                }
            }
            _lockSelection = false;
        }
    }
}
