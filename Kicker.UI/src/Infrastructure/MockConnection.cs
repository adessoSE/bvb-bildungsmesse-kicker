using System;
using System.Reactive.Disposables;
using Kicker.Domain;

namespace Kicker.UI.Infrastructure
{
    public class MockConnection : IObservable<IConnectionEvent>
    {
        private Game _game;

        public IDisposable Subscribe(IObserver<IConnectionEvent> observer)
        {
            observer.OnNext(new ConnectionEvent.Connected());
            
            var settings = GameSettings.defaultSettings;
            _game = GameModule.create(settings);
            var state = GameModule.getState(_game);
            
            observer.OnNext(new ConnectionEvent.Notification(GameNotification.NewState(state)));
            
            return Disposable.Empty;
        }
    }
}