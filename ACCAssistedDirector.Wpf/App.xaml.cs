using AppAnalytics;
using MvvmCross.Core;
using MvvmCross.Platforms.Wpf.Core;
using MvvmCross.Platforms.Wpf.Views;

namespace ACCAssistedDirector.Wpf {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : MvxApplication {
        protected override void RegisterSetup()
        {
            this.RegisterSetupType<MvxWpfSetup<ACCAssistedDirector.Core.App>>();
            //EventsTracker.StartTracker();
        }
    }
}
