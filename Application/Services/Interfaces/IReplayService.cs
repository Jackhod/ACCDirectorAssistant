using Application.Services.Interfaces;
using Domain.Models;

namespace ACCAssistedDirector.Core.Services {

    public delegate void EventAddedDelegate(BroadcastingEventModel broadcastingEvent);
    public delegate void EventRemovedDelegate(BroadcastingEventModel broadcastingEvent);

    public interface IReplayService : Service{

        public void PlayQuickReplay(int durationSeconds, int secondsBack, int targetCarId);
        public void PlayHighlightReplay(int durationSeconds, int startSeconds, int targetCarId);
        public void RemoveEvent(BroadcastingEventModel evnt);

        public event EventAddedDelegate OnEventAdded;
        public event EventRemovedDelegate OnEventRemoved;
    }
}
