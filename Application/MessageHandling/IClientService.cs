using Domain.ACCUpdatesStructs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ACCAssistedDirector.Core.MessageHandling {

    public interface IClientService : IDisposable {
        public IMessageHandler MessageHandler { get; }
        public string IpPort { get; }
        public string DisplayName { get; }
        public string ConnectionPassword { get; }
        public string CommandPassword { get; }
        public int MsRealtimeUpdateInterval { get; }
        
        public void Init(string ip, int port, string displayName, string connectionPassword, string commandPassword, int msRealtimeUpdateInterval);
        public void Connect();
        public void Shutdown();
        public Task ShutdownAsnyc();
    }
}
