using AppAnalytics;
using MvvmCross.Platforms.Wpf.Views;

namespace ACCAssistedDirector.Wpf {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MvxWindow {
        public MainWindow()
        {
            InitializeComponent();
            Title = "ACCDirectorAssistant";
        }

        private void ClosingWindow(object sender, System.ComponentModel.CancelEventArgs e) {
            //Uncomment to stop azure tracking
            //EventsTracker.EndTracker();
        }
    }
}
