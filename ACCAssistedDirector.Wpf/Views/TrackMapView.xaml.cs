using MvvmCross.Platforms.Wpf.Views;
using System;
using System.Collections.Generic;
using ACCAssistedDirector.Core.ViewModels;
using System.Diagnostics;
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

namespace ACCAssistedDirector.Wpf.Views {
    /// <summary>
    /// Logica di interazione per TrackMapView.xaml
    /// </summary>
    public partial class TrackMapView : MvxWpfView {
        public TrackMapView() {
            InitializeComponent();
        }

        private void LenghtChanged(object sender, SizeChangedEventArgs e) {
            MainViewModel.TrackMapVM.Length = (float)e.NewSize.Height;
        }
    }
}
