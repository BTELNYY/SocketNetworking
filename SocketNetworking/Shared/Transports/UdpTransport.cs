using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace SocketNetworking.Shared.Transports
{
    public class UdpTransport : NetworkTransport
    {
        public override IPEndPoint Peer
        {
            get
            {
                if (IsServerMode)
                {
                    return _emulatedPeer;
                }
                if (_hasConnected)
                {
                    return Client.Client.RemoteEndPoint as IPEndPoint;
                }
                return _peer;
            }
        }

        private IPEndPoint _peer = new IPEndPoint(IPAddress.Any, 0);

        public override IPEndPoint LocalEndPoint => Client.Client.LocalEndPoint as IPEndPoint;

        public override IPAddress PeerAddress => Peer.Address;

        public override int PeerPort => Peer.Port;

        public bool OverrideConnectedState { get; set; } = false;

        public bool OverrideConnectedStateValue { get; set; } = true;

        public override bool IsConnected
        {
            get
            {
                if (IsServerMode)
                {
                    return _serverIsConnected;
                }
                if (OverrideConnectedState)
                {
                    return OverrideConnectedStateValue;
                }
                if (_hasConnected)
                {
                    if (Client == null)
                    {
                        return false;
                    }
                    if (Client.Client == null)
                    {
                        return false;
                    }
                    return Client.Client.Connected;
                }
                return false;
            }
        }

        protected IPEndPoint _emulatedPeer = new IPEndPoint(IPAddress.Any, 0);

        protected IPEndPoint _emulatedMe = new IPEndPoint(0, 0);

        protected bool _isServerMode = false;

        public bool IsServerMode
        {
            get
            {
                return _isServerMode;
            }
        }

        private bool _serverIsConnected = false;

        public virtual void SetupForServerUse(IPEndPoint peer, IPEndPoint me)
        {
            _isServerMode = true;
            _serverIsConnected = true;
            _emulatedPeer = peer;
            _emulatedMe = me;
        }

        public virtual void ServerReceive(byte[] data, IPEndPoint endPoint)
        {
            _receivedBytes.Enqueue((data, endPoint));
        }

        ConcurrentQueue<ValueTuple<byte[], IPEndPoint>> _receivedBytes = new ConcurrentQueue<ValueTuple<byte[], IPEndPoint>>();

        public override bool DataAvailable
        {
            get
            {
                if (IsServerMode)
                {
                    return _receivedBytes.Count > 0;
                }
                else
                {
                    return Client.Available > 0;
                }
            }
        }

        public override int DataAmountAvailable
        {
            get
            {
                if (IsServerMode)
                {
                    return _receivedBytes.ElementAt(0).Item1.Length;
                }
                else
                {
                    return Client.Available;
                }
            }
        }

        public IPEndPoint BroadcastEndpoint
        {
            get
            {
                return new IPEndPoint(0, 0);
            }
        }

        public bool AllowBroadcast
        {
            get
            {
                return Client.EnableBroadcast;
            }
            set
            {
                Client.EnableBroadcast = value;
            }
        }

        public override Socket Socket => Client.Client;

        public UdpClient Client { get; set; } = new UdpClient();

        private List<IPAddress> _multiCastGroups = new List<IPAddress>();

        private bool _hasConnected = false;

        public void JoinMulticastGroup(IPAddress multicastGroup)
        {
            if (_hasConnected)
            {
                Log.GlobalWarning("Can't join multicast groups if connected.");
                return;
            }
            _multiCastGroups.Add(multicastGroup);
            Client.JoinMulticastGroup(multicastGroup);
        }

        public void DropMulticastGroup(IPAddress multicastGroup)
        {
            if (_hasConnected)
            {
                Log.GlobalWarning("Can't join multicast groups if connected.");
                return;
            }
            _multiCastGroups.Remove(multicastGroup);
            Client.DropMulticastGroup(multicastGroup);
        }

        public bool InMulticastGroup(IPAddress multicastGroup)
        {
            return _multiCastGroups.Contains(multicastGroup);
        }

        public override void Close()
        {
            if (_isServerMode)
            {
                Client = null;
                _serverIsConnected = false;
                return;
            }
            Client?.Close();
            Client?.Dispose();
            Client = null;
        }

        public override Exception Connect(string hostname, int port)
        {
            try
            {
                Client.Connect(hostname, port);
                _hasConnected = true;
                _peer = new IPEndPoint(IPAddress.Parse(hostname), port);
                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        public override (byte[], Exception, IPEndPoint) Receive()
        {
            if (IsServerMode)
            {
                while (_receivedBytes.IsEmpty)
                {
                    //do nothing.
                }
                _receivedBytes.TryDequeue(out (byte[], IPEndPoint) result);
                //Log.GlobalDebug(result.Item1.Length.ToString() + " ServerMode");
                ReceivedBytes += (ulong)result.Item1.Length;
                return (result.Item1, null, _emulatedPeer);
            }
            else
            {
                try
                {
                    byte[] read = new byte[] { };
                    IPEndPoint peer;
                    if (AllowBroadcast)
                    {
                        peer = BroadcastEndpoint;
                        //Log.GlobalDebug("Waiting for BroadcastEndpoint");
                        read = Client.Receive(ref peer);
                    }
                    else
                    {
                        peer = Peer;
                        //Log.GlobalDebug($"Waiting for RemoteEndPoint. EndPoint: ${peer.Address}:{peer.Port}");
                        read = Client.Receive(ref peer);
                    }
                    //Log.GlobalDebug(read.Length.ToString() + " ClientMode");
                    ReceivedBytes += (ulong)read.Length;
                    return (read, null, peer);
                }
                catch (Exception ex)
                {
                    return (null, ex, null);
                }
            }
        }

        public override Exception Send(byte[] data, IPEndPoint destination)
        {
            //Log.GlobalDebug($"Sending data on: {LocalEndPoint.Address}:{LocalEndPoint.Port}, To: {destination.Address}:{destination.Port}");
            try
            {
                if (_hasConnected)
                {
                    Log.GlobalWarning("Tried to send data to random host while connected!");
                    Send(data);
                    return null;
                }
                int sent;
                if (IsServerMode)
                {
                    sent = Client.Send(data, data.Length, _emulatedPeer);
                }
                else
                {
                    sent = Client.Send(data, data.Length, _peer);
                }
                SentBytes += (ulong)sent;
                //Log.GlobalDebug("Bytes Sent: " + sent);
                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        public override Exception Send(byte[] data)
        {
            try
            {
                Client.Send(data, data.Length);
                SentBytes += (ulong)data.Length;
                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        public Exception SendBroadcast(byte[] data)
        {
            try
            {
                Client.Send(data, data.Length, BroadcastEndpoint);
                SentBytes += (ulong)data.Length;
                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        public async override Task<Exception> SendAsync(byte[] data, IPEndPoint destination)
        {
            try
            {
                if (_hasConnected)
                {
                    Log.GlobalWarning("Tried to send data to random host while connected!");
                    Send(data);
                    return null;
                }
                int sent;
                if (IsServerMode)
                {
                    sent = await Client.SendAsync(data, data.Length, _emulatedPeer);
                }
                else
                {
                    sent = await Client.SendAsync(data, data.Length, _peer);
                }
                SentBytes += (ulong)sent;
                //Log.GlobalDebug("Bytes Sent: " + sent);
                return null;
            }
            catch (Exception e)
            {
                return e;
            }
        }

        public async override Task<Exception> SendAsync(byte[] data)
        {
            try
            {
                int sent = await Client.SendAsync(data, data.Length);
                SentBytes += (ulong)sent;
                return null;
            }
            catch (Exception e)
            {
                return e;
            }
        }

        public async override Task<(byte[], Exception, IPEndPoint)> ReceiveAsync()
        {
            if (IsServerMode)
            {
                while (_receivedBytes.IsEmpty)
                {
                    //do nothing.
                }
                _receivedBytes.TryDequeue(out (byte[], IPEndPoint) result);
                //Log.GlobalDebug(result.Item1.Length.ToString() + " ServerMode");
                ReceivedBytes += (ulong)result.Item1.Length;
                return (result.Item1, null, _emulatedPeer);
            }
            else
            {
                try
                {
                    byte[] read = new byte[] { };
                    IPEndPoint peer;
                    UdpReceiveResult result;
                    if (AllowBroadcast)
                    {
                        peer = BroadcastEndpoint;
                        //Log.GlobalDebug("Waiting for BroadcastEndpoint");
                        result = await Client.ReceiveAsync();
                        read = result.Buffer;
                        peer = result.RemoteEndPoint;
                    }
                    else
                    {
                        peer = Peer;
                        //Log.GlobalDebug($"Waiting for RemoteEndPoint. EndPoint: ${peer.Address}:{peer.Port}");
                        result = await Client.ReceiveAsync();
                        read = result.Buffer;
                        peer = result.RemoteEndPoint;
                    }
                    ReceivedBytes += (ulong)read.Length;
                    //Log.GlobalDebug(read.Length.ToString() + " ClientMode");
                    return (read, null, peer);
                }
                catch (Exception ex)
                {
                    return (null, ex, null);
                }
            }
        }
    }
}
