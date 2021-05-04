using Domain.Models;

namespace ACCAssistedDirector.Core.Services {

    public delegate void EventAddedDelegate(BroadcastingEventModel broadcastingEvent);
    public delegate void EventRemovedDelegate(BroadcastingEventModel broadcastingEvent);

    public interface IReplayService {

        public void RemoveEvent(BroadcastingEventModel evnt);

        public event EventAddedDelegate OnEventAdded;
        public event EventRemovedDelegate OnEventRemoved;
    }
}
