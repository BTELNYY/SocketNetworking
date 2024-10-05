using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SocketNetworking.Transports
{
    public class UdpTransport : NetworkTransport
    {
        public override IPEndPoint Peer
        {
            get
            {
                if(IsServerMode)
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
                    return true;
                }
                if (OverrideConnectedState)
                {
                    return OverrideConnectedStateValue;
                }
                if(_hasConnected)
                {
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

        public virtual void SetupForServerUse(IPEndPoint peer, IPEndPoint me)
        {
            _isServerMode = true;
            _emulatedPeer = peer;
            _emulatedMe = me;
        }

        public virtual void ServerRecieve(byte[] data)
        {
            _receivedBytes.Enqueue(data);
        }

        ConcurrentQueue<byte[]> _receivedBytes = new ConcurrentQueue<byte[]>();

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
            Client.Close();
        }

        public override Exception Connect(string hostname, int port)
        {
            try
            {
                Client.Connect(hostname, port);
                _hasConnected = true;
                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        public override (byte[], Exception, IPEndPoint) Receive(int offset, int size)
        {
            if (IsServerMode)
            {
                while (_receivedBytes.IsEmpty)
                {
                    //do nothing.
                }
                _receivedBytes.TryDequeue(out byte[] result);
                Log.GlobalDebug(result.Length.ToString() + " ServerMode");
                return (result, null, _emulatedPeer);
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
                        Log.GlobalDebug("Waiting for BroadcastEndpoint");
                        read = Client.Receive(ref peer);
                    }
                    else
                    {
                        peer = Peer;
                        Log.GlobalDebug($"Waiting for RemoteEndPoint. EndPoint: ${peer.Address}:{peer.Port}");
                        read = Client.Receive(ref peer);
                    }
                    Log.GlobalDebug(read.Length.ToString() + " ClientMode");
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
            Log.GlobalDebug($"Sending data on: {LocalEndPoint.Address}:{LocalEndPoint.Port}, To: {destination.Address}:{destination.Port}");
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
                    return null;
                }
                else
                {
                    sent = Client.Send(data, data.Length, _peer);
                }
                Log.GlobalDebug("Bytes Sent: " + sent);
                return null;
            }
            catch(Exception ex)
            {
                return ex;
            }
        }

        public override Exception Send(byte[] data)
        {
            try
            {
                Client.Send(data, data.Length);
                return null;
            }
            catch(Exception ex)
            {
                return ex;
            }
        }

        public Exception SendBroadcast(byte[] data)
        {
            try
            {
                Client.Send(data, data.Length, BroadcastEndpoint);
                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }
    }
}
