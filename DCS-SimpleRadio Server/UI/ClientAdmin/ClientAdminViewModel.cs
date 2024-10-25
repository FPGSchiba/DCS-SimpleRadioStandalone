﻿using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Caliburn.Micro;
using Ciribob.DCS.SimpleRadio.Standalone.Server.Network;
using NLog;
using LogManager = NLog.LogManager;

namespace Ciribob.DCS.SimpleRadio.Standalone.Server.UI.ClientAdmin
{
    public sealed class ClientAdminViewModel : Screen, IHandle<ServerStateMessage>
    {
        private static readonly TimeSpan LastTransmissionThreshold = TimeSpan.FromMilliseconds(200);

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly IEventAggregator _eventAggregator;
        private readonly DispatcherTimer _updateTimer;

        public ClientAdminViewModel(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
            _eventAggregator.Subscribe(this);

            DisplayName = "SR Client List";

            _updateTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
            _updateTimer.Tick += _updateTimer_Tick;
        }

        public ObservableCollection<ClientViewModel> Clients { get; } = new ObservableCollection<ClientViewModel>();

        protected override Task OnActivateAsync(CancellationToken token)
        {
            _updateTimer?.Start();

            return base.OnActivateAsync(token);
        }

        protected override Task OnDeactivateAsync(bool close, CancellationToken token)
        {
            if (close)
            {
                _updateTimer?.Stop();
            }

            return base.OnDeactivateAsync(close, token);
        }

        public void Handle(ServerStateMessage message)
        {
            Clients.Clear();

            message.Clients.Apply(client => Clients.Add(new ClientViewModel(client, _eventAggregator)));
        }

        public Task HandleAsync(ServerStateMessage message, CancellationToken token)
        {
            return new Task(() => Handle(message), token);
        }

        private void _updateTimer_Tick(object sender, EventArgs e)
        {
            foreach (ClientViewModel client in Clients)
            {
                if ((DateTime.Now - client.Client.LastTransmissionReceived) >= LastTransmissionThreshold)
                {
                    client.Client.TransmittingFrequency = "---";
                }
            }
        }
    }
}