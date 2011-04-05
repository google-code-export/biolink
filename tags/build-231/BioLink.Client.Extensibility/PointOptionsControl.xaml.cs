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
using BioLink.Client.Utilities;

namespace BioLink.Client.Extensibility {
    /// <summary>
    /// Interaction logic for PointOptionsControl.xaml
    /// </summary>
    public partial class PointOptionsControl : UserControl {

        internal static List<PointShapeViewModel> _shapeModel = new List<PointShapeViewModel>();

        static PointOptionsControl() {
            _shapeModel.Add(new PointShapeViewModel(MapPointShape.Circle));
            _shapeModel.Add(new PointShapeViewModel(MapPointShape.Square));
            _shapeModel.Add(new PointShapeViewModel(MapPointShape.Triangle));
        }

        public PointOptionsControl() {
            init(null);
        }

        public PointOptionsControl(SymbolInfo info) {
            init(info);
        }

        private void init(SymbolInfo info) {
            InitializeComponent();
            
            cmbShape.ItemsSource = _shapeModel;            
            cmbShape.SelectionChanged += new SelectionChangedEventHandler(cmbShape_SelectionChanged);
            Loaded += new RoutedEventHandler(PointOptionsControl_Loaded);

            if (info == null) {
                sizeSlider.Value = 7;
                cmbShape.SelectedIndex = 0;
                ctlColor.SelectedColor = Colors.Red;
                this.Shape = MapPointShape.Circle;
            } else {
                sizeSlider.Value = info.Size;                
                cmbShape.SelectedItem = _shapeModel.Find((vm) => {
                    return vm.Shape == info.Shape;
                });
                ctlColor.SelectedColor = Color.FromArgb(info.Color.A, info.Color.R, info.Color.G, info.Color.B);
                this.Shape = info.Shape;
            }
        }

        void PointOptionsControl_Loaded(object sender, RoutedEventArgs e) {
            UpdatePreview();
        }

        void cmbShape_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            var vm = cmbShape.SelectedItem as PointShapeViewModel;
            if (vm != null) {
                this.Shape = vm.Shape;
            }
            this.UpdatePreview();
        }

        private void ctlColor_SelectedColorChanged(Color obj) {
            UpdatePreview();
        }

        private void UpdatePreview() {
            if (IsLoaded) {
                var image = MapSymbolGenerator.GetSymbol(Shape, (int)sizeSlider.Value, ctlColor.SelectedColor, chkDrawOutline.IsChecked.ValueOrFalse(), Colors.Black);
                BitmapSource s = GraphicsUtils.SystemDrawingImageToBitmapSource(image);
                imgPreview.Source = s;
                if (ValuesChanged != null) {
                    ValuesChanged(this);
                }
            }
        }

        private void chkDrawOutline_Checked(object sender, RoutedEventArgs e) {
            UpdatePreview();
        }

        private void chkDrawOutline_Unchecked(object sender, RoutedEventArgs e) {
            UpdatePreview();
        }

        private void slider1_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            UpdatePreview();
        }

        public MapPointShape Shape { get; set; }

        public int Size {
            get { return (int)sizeSlider.Value; }
        }

        public Color Color {
            get { return ctlColor.SelectedColor; }
        }

        public bool DrawOutline {
            get { return chkDrawOutline.IsChecked.ValueOrFalse(); }
        }

        protected IMapPointSetGenerator Generator { get; private set; }

        public event Action<PointOptionsControl> ValuesChanged;
    }

    public class PointShapeConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            MapPointShape? s = (MapPointShape?)value;
            var vm = PointOptionsControl._shapeModel.Find((cand) => {
                return cand.Shape == s;
            });
            return vm;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            var vm = value as PointShapeViewModel;
            return vm.Shape;
        }
    }

    public class PointShapeViewModel : ViewModelBase {

        public PointShapeViewModel(MapPointShape shape) {
            this.Shape = shape;
            this.Icon = GraphicsUtils.SystemDrawingImageToBitmapSource(MapSymbolGenerator.GetSymbol(shape, 10, Colors.Black, true));
        }

        public MapPointShape Shape { get; set; }


        public override int? ObjectID {
            get { return 0; }
        }
    }

}
