using SocketNetworking.PacketSystem;
using SocketNetworking.Shared;
using SocketNetworking.Transports;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SocketNetworking.Client
{
    public class TcpNetworkClient : NetworkClient
    {
        public override NetworkTransport Transport
        {
            get
            {
                return base.Transport;
            }
            set
            {
                if(value is TcpTransport tcp)
                {
                    base.Transport = tcp;
                }
                else
                {
                    throw new InvalidOperationException("TcpNetworkClient does not support non-tcp transport.");
                }
            }
        }

        public TcpTransport TcpTransport
        {
            get
            {
                return (TcpTransport)Transport;
            }
            set
            {
                Transport = value;
            }
        }

        public bool TcpNoDelay
        {
            get
            {
                return TcpTransport.Client.NoDelay;
            }
            set
            {
                TcpTransport.Client.NoDelay = value;
            }
        }

        protected override void PacketReaderThreadMethod()
        {
            Log.GlobalInfo($"Client thread started, ID {ClientID}");
            //int waitingSize = 0;
            //byte[] prevPacketFragment = { };
            byte[] buffer = new byte[Packet.MaxPacketSize]; // this can now be freely changed
            Transport.BufferSize = Packet.MaxPacketSize;
            int fillSize = 0; // the amount of bytes in the buffer. Reading anything from fillsize on from the buffer is undefined.
            while (true)
            {
            Packet: // this is for breaking a nested loop further down. thanks C#
                if (_shuttingDown)
                {
                    Log.GlobalInfo("Shutting down loop");
                    break;
                }
                if (!IsConnected)
                {
                    Log.GlobalDebug("Disconnected!");
                    StopClient();
                    return;
                }
                /*if(TcpClient.ReceiveBufferSize == 0)
                {
                    continue;
                }*/
                /*if (!NetworkStream.DataAvailable)
                {
                    //Log.Debug("Nothing to read on stream");
                    continue;
                }*/
                //Log.Debug(TcpClient.ReceiveBufferSize.ToString());
                if (fillSize < sizeof(int))
                {
                    // we dont have enough data to read the length data
                    //Log.Debug($"Trying to read bytes to get length (we need at least 4 we have {fillSize})!");
                    int count = 0;
                    try
                    {
                        int tempFillSize = fillSize;
                        //(byte[], Exception) transportRead = Transport.Receive(fillSize, buffer.Length - fillSize);
                        if (TcpNoDelay)
                        {
                            (byte[], Exception, IPEndPoint) transportRead = Transport.Receive(0, buffer.Length - fillSize);
                            count = transportRead.Item1.Length;
                            buffer = Transport.Buffer;
                        }
                        else
                        {
                            (byte[], Exception, IPEndPoint) transportRead = Transport.Receive(fillSize, buffer.Length - fillSize);
                            count = transportRead.Item1.Length;
                            buffer = Transport.Buffer;
                        }
                        //count = NetworkStream.Read(tempBuffer, 0, buffer.Length - fillSize);
                    }
                    catch (Exception ex)
                    {
                        Log.GlobalError(ex.ToString());
                        continue;
                    }
                    fillSize += count;
                    //Log.Debug($"Read {count} bytes from buffer ({fillSize})!");
                    continue;
                }
                int bodySize = BitConverter.ToInt32(buffer, 0); // i sure do hope this doesnt modify the buffer.
                bodySize = IPAddress.NetworkToHostOrder(bodySize);
                if (bodySize == 0)
                {
                    Log.GlobalWarning("Got a malformed packet, Body Size can't be 0, Resetting header to beginning of Packet (may cuase duplicate packets)");
                    fillSize = 0;
                    continue;
                }
                fillSize -= sizeof(int); // this kinda desyncs fillsize from the actual size of the buffer, but eh
                // read the rest of the whole packet
                if (bodySize > Packet.MaxPacketSize || bodySize < 0)
                {
                    CurrentConnectionState = ConnectionState.Disconnected;
                    string s = string.Empty;
                    for (int i = 0; i < buffer.Length; i++)
                    {
                        s += Convert.ToString(buffer[i], 2).PadLeft(8, '0') + " ";
                    }
                    Log.GlobalError("Body Size is corrupted! Raw: " + s);
                }
                while (fillSize < bodySize)
                {
                    //Log.Debug($"Trying to read bytes to read the body (we need at least {bodySize} and we have {fillSize})!");
                    if (fillSize == buffer.Length)
                    {
                        // The buffer is too full, and we are fucked (oh shit)
                        Log.GlobalError("Buffer became full before being able to read an entire packet. This probably means a packet was sent that was bigger then the buffer (Which is the packet max size). This is not recoverable, Disconnecting!");
                        Disconnect("Illegal Packet Size");
                        break;
                    }
                    int count;
                    try
                    {
                        
                        (byte[], Exception, IPEndPoint) transportRead = Transport.Receive(fillSize, buffer.Length - fillSize);
                        count = transportRead.Item1.Length;
                        buffer = Transport.Buffer;
                        //count = NetworkStream.Read(buffer, fillSize, buffer.Length - fillSize);
                    }
                    catch (Exception ex)
                    {
                        Log.GlobalError(ex.ToString());
                        goto Packet;
                    }
                    fillSize += count;
                }
                // we now know we have enough bytes to read at least one whole packet;
                byte[] fullPacket = ShiftOut(ref buffer, bodySize + sizeof(int));
                if ((fillSize -= bodySize) < 0)
                {
                    fillSize = 0;
                }
                //fillSize -= bodySize; // this resyncs fillsize with the fullness of the buffer
                //Log.Debug($"Read full packet with size: {fullPacket.Length}");
                PacketHeader header = Packet.ReadPacketHeader(fullPacket);
                if (header.Type == PacketType.CustomPacket && NetworkManager.GetCustomPacketByID(header.CustomPacketID) == null)
                {
                    Log.GlobalWarning($"Got a packet with a Custom Packet ID that does not exist, either not registered or corrupt. Custom Packet ID: {header.CustomPacketID}, Target: {header.NetworkIDTarget}");
                }
                Log.GlobalDebug("Active Flags: " + string.Join(", ", header.Flags.GetActiveFlags()));
                Log.GlobalDebug($"Inbound Packet Info, Size Of Full Packet: {header.Size}, Type: {header.Type}, Target: {header.NetworkIDTarget}, CustomPacketID: {header.CustomPacketID}");
                byte[] rawPacket = fullPacket;
                byte[] headerBytes = fullPacket.Take(PacketHeader.HeaderLength).ToArray();
                byte[] packetBytes = fullPacket.Skip(PacketHeader.HeaderLength).ToArray();
                int currentEncryptionState = (int)EncryptionState;
                if (header.Flags.HasFlag(PacketFlags.SymetricalEncrypted))
                {
                    Log.GlobalDebug("Trying to decrypt a packet using SYMMETRICAL encryption!");
                    if (currentEncryptionState < (int)EncryptionState.SymmetricalReady)
                    {
                        Log.GlobalError("Encryption cannot be done at this point: Not ready.");
                        return;
                    }
                    packetBytes = EncryptionManager.Decrypt(packetBytes);
                }
                if (header.Flags.HasFlag(PacketFlags.AsymtreicalEncrypted))
                {
                    Log.GlobalDebug("Trying to decrypt a packet using ASYMMETRICAL encryption!");
                    if (currentEncryptionState < (int)EncryptionState.AsymmetricalReady)
                    {
                        Log.GlobalError("Encryption cannot be done at this point: Not ready.");
                        return;
                    }
                    packetBytes = EncryptionManager.Decrypt(packetBytes, false);
                }
                if (header.Flags.HasFlag(PacketFlags.Compressed))
                {
                    packetBytes = packetBytes.Decompress();
                }
                if (header.Size + 4 < fullPacket.Length)
                {
                    Log.GlobalWarning($"Header provided size is less then the actual packet length! Header: {header.Size}, Actual Packet Size: {fullPacket.Length - 4}");
                }
                fullPacket = headerBytes.Concat(packetBytes).ToArray();
                InvokePacketRead(header, fullPacket);
                if (ManualPacketHandle)
                {
                    ReadPacketInfo packetInfo = new ReadPacketInfo()
                    {
                        Header = header,
                        Data = fullPacket
                    };
                    _toReadPackets.Enqueue(packetInfo);
                    InvokePacketReadyToHandle(packetInfo.Header, packetInfo.Data);
                }
                else
                {
                    HandlePacket(header, fullPacket);
                }
            }
            Log.GlobalInfo("Shutting down client, Closing socket.");
            Transport.Close();
        }
    }
}
