﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using BioLink.Client.Extensibility;
using BioLink.Client.Utilities;
using BioLink.Data;
using BioLink.Data.Model;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace BioLink.Client.Tools {
    /// <summary>
    /// Interaction logic for FindReferencesControl.xaml
    /// </summary>
    public partial class ReferenceManager : DatabaseActionControl, ISelectionHostControl {

        private GridViewColumnHeader _lastHeaderClicked = null;
        private ListSortDirection _lastDirection = ListSortDirection.Ascending;
        private ReferenceFavorites _favorites;

        #region Designer Constructor
        public ReferenceManager() {
            InitializeComponent();
        }
        #endregion

        public ReferenceManager(User user, ToolsPlugin owner) 
            : base(user, "ReferenceManager") {
            InitializeComponent();
            this.Owner = owner;
            lvwResults.SelectionChanged += new SelectionChangedEventHandler((sender, e) => {
                var item = lvwResults.SelectedItem as ReferenceSearchResultViewModel;
                txtRTF.DataContext = item;
            });

            ChangesCommitted += new PendingChangesCommittedHandler(ReferenceManager_ChangesCommitted);

            lvwResults.PreviewMouseRightButtonUp += new MouseButtonEventHandler(lvwResults_PreviewMouseRightButtonUp);

            ListViewDragHelper.Bind(lvwResults, ListViewDragHelper.CreatePinnableGenerator(ToolsPlugin.TOOLS_PLUGIN_NAME, LookupType.Reference));

            lvwResults.AddHandler(GridViewColumnHeader.ClickEvent, new RoutedEventHandler(GridViewColumnHeaderClickedHandler));

            _favorites = new ReferenceFavorites(user, this);
            tabReferences.AddTabItem("Favorites", _favorites);
        }

        private void GridViewColumnHeaderClickedHandler(object sender, RoutedEventArgs e) {
            GridViewColumnHeader headerClicked = e.OriginalSource as GridViewColumnHeader;
            ListSortDirection direction;

            if (headerClicked != null) {
                if (headerClicked.Role != GridViewColumnHeaderRole.Padding) {
                    if (headerClicked != _lastHeaderClicked) {
                        direction = ListSortDirection.Ascending;
                    } else {
                        if (_lastDirection == ListSortDirection.Ascending) {
                            direction = ListSortDirection.Descending;
                        } else {
                            direction = ListSortDirection.Ascending;
                        }
                    }

                    String b = headerClicked.Content as String;
                    Sort(b, direction);

                    if (direction == ListSortDirection.Ascending) {
                        headerClicked.Column.HeaderTemplate = Resources["HeaderTemplateArrowUp"] as DataTemplate;
                    } else {
                        headerClicked.Column.HeaderTemplate = Resources["HeaderTemplateArrowDown"] as DataTemplate;
                    }

                    // Remove arrow from previously sorted header
                    if (_lastHeaderClicked != null && _lastHeaderClicked != headerClicked) {
                        _lastHeaderClicked.Column.HeaderTemplate = null;
                    }

                    _lastHeaderClicked = headerClicked;
                    _lastDirection = direction;
                }
            }
        }

        private void Sort(String columnName, ListSortDirection direction) {
            string memberName = "";
            switch (columnName) {
                case "Code":
                    memberName = "RefCode";
                    break;
                case "Year":
                    memberName = "YearOfPub";
                    break;
                default:
                    memberName = columnName;
                    break;
            }

            ListCollectionView dataView = CollectionViewSource.GetDefaultView(lvwResults.ItemsSource) as ListCollectionView;
            dataView.SortDescriptions.Clear();
            SortDescription sd = new SortDescription(memberName, direction);
            dataView.SortDescriptions.Add(sd);
            dataView.Refresh();
        }

        void ReferenceManager_ChangesCommitted(object sender) {
            // Redo the search...
            DoSearch();
            // and reload the favorites...
            _favorites.ReloadFavorites();
        }

        void lvwResults_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e) {
            ContextMenuBuilder builder = new ContextMenuBuilder(null);

            builder.New("Add New").Handler(() => AddNew()).End();
            builder.Separator();
            builder.New("Delete").Handler(() => DeleteSelected()).End();
            builder.Separator();
            MenuItemBuilder b = new MenuItemBuilder();
            var selected = lvwResults.SelectedItem as ReferenceSearchResultViewModel;
            var mnuFav = b.New("Add to Favorites").MenuItem;
            mnuFav.Items.Add(b.New("User specific").Handler(() => { _favorites.AddToFavorites(selected, false); }).MenuItem);
            mnuFav.Items.Add(b.New("Global").Handler(() => { _favorites.AddToFavorites(selected, true); }).MenuItem);
            builder.AddMenuItem(mnuFav);
            builder.Separator();
            builder.New("Pin to pinboard").Handler(() => { PinSelected(); }).End();
            builder.New("Edit").Handler(() => EditSelected()).End();

            lvwResults.ContextMenu = builder.ContextMenu;
        }

        private void EditSelected() {
            var selected = lvwResults.SelectedItem as ReferenceSearchResultViewModel;
            if (selected != null) {
                Owner.EditReference(selected.RefID);
            }
        }

        private void AddNew() {
            Owner.AddNewReference();            
        }

        private void DeleteSelected() {
            var selected = lvwResults.SelectedItem as ReferenceSearchResultViewModel;
            if (selected != null) {                
                DeleteReference(selected.RefID, selected.RefCode);
                selected.IsDeleted = true;
            }

        }

        public void DeleteReference(int refID, String refCode) {
            if (this.Question(string.Format("Are you sure you wish to permanently delete the reference '{0}'?", refCode), "Delete Reference?")) {
                RegisterUniquePendingChange(new DeleteReferenceCommand(refID));
            }
        }

        private void PinSelected() {
            var selected = lvwResults.SelectedItem as ReferenceSearchResultViewModel;
            if (selected != null) {
                var pinnable = new PinnableObject(ToolsPlugin.TOOLS_PLUGIN_NAME, LookupType.Reference, selected.RefID, selected.DisplayLabel);
                PluginManager.Instance.PinObject(pinnable);
            }
        }

        public SelectionResult Select() {
            var item = lvwResults.SelectedItem as ReferenceSearchResultViewModel;

            if (item != null) {
                var res = new ReferenceSelectionResult(item.Model);
                return res;
            }
            return null;

        }

        private ObservableCollection<ReferenceSearchResultViewModel> _searchModel;

        private void DoSearch() {

            if (_searchModel == null) {
                _searchModel = new ObservableCollection<ReferenceSearchResultViewModel>();
                lvwResults.ItemsSource = _searchModel;
            }

            _searchModel.Clear();

            if (string.IsNullOrEmpty(txtAuthor.Text) && string.IsNullOrEmpty(txtCode.Text) && string.IsNullOrEmpty(txtOther.Text) && string.IsNullOrEmpty(txtYear.Text)) {
                ErrorMessage.Show("Please enter some search criteria");
                return;
            }

            List<ReferenceSearchResult> data = Service.FindReferences(Wildcard(txtCode.Text), Wildcard(txtAuthor.Text), Wildcard(txtYear.Text), Wildcard(txtOther.Text));

            lblStatus.Content = string.Format("{0} matching references found.", data.Count);

            data.ForEach(item => {
                _searchModel.Add(new ReferenceSearchResultViewModel(item));
            });
            
        }

        private string Wildcard(string str) {
            if (String.IsNullOrEmpty(str)) {
                return null;
            }

            return str + "%";
        }

        protected SupportService Service { get { return new SupportService(User); } }

        public ToolsPlugin Owner { get; private set; }

        private void btnFind_Click(object sender, RoutedEventArgs e) {
            DoSearch();
        }

        private void btnProperties_Click(object sender, RoutedEventArgs e) {
            EditSelected();
        }

        private void btnAddNew_Click(object sender, RoutedEventArgs e) {
            AddNew();
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e) {
            DeleteSelected();
        }
    }

    public class ReferenceSelectionResult : SelectionResult {

        public ReferenceSelectionResult(ReferenceSearchResult data) {
            this.DataObject = data;
            this.Description = data.RefCode;
            this.ObjectID = data.RefID;
        }
        
    }

    public class ReferenceSearchResultViewModel : GenericHierarchicalViewModelBase<ReferenceSearchResult> {

        public ReferenceSearchResultViewModel(ReferenceSearchResult model) : base(model, ()=>model.RefID) { }

        public override string DisplayLabel {
            get { return String.Format("{0}, {1} [{2}] ({3})", Title, Author, YearOfPub, RefCode); }
        }

        public override FrameworkElement TooltipContent {
            get {
                return new ReferenceTooltipContent(RefID, this);
            }
        }

        public int RefID {
            get { return Model.RefID; }
            set { SetProperty(() => Model.RefID, value); }
        }

        public string RefCode {
            get { return Model.RefCode; }
            set { SetProperty(() => Model.RefCode, value); }
        }

        public string Author {
            get { return Model.Author; }
            set { SetProperty(() => Model.Author, value); }
        }

        public string YearOfPub {
            get { return Model.YearOfPub; }
            set { SetProperty(() => Model.YearOfPub, value); }
        }

        public string Title {
            get { return Model.Title; }
            set { SetProperty(() => Model.Title, value); }
        }

        public string RefType {
            get { return Model.RefType; }
            set { SetProperty(() => Model.RefType, value); } 
        }

        public string RefRTF {
            get { return Model.RefRTF; }
            set { SetProperty(() => Model.RefRTF, value); }
        }

    }


}
