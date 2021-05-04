using System;
using System.Collections.Generic;
using System.Text;

namespace ACCAssistedDirector.Core.Services.Interfaces {

    public delegate void HUDPagesReceivedDelegate(string HUDPage);
    public delegate void ActiveHUDPageUpdateDelegate(string activeHUD);

    public interface IHUDService {

        public event HUDPagesReceivedDelegate OnHUDPageReceived;
        public event ActiveHUDPageUpdateDelegate OnActiveHUDPageUpdated;
    }
}
