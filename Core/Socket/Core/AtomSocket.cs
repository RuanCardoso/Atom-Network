/*===========================================================
    Author: Ruan Cardoso
    -
    Country: Brazil(Brasil)
    -
    Contact: cardoso.ruan050322@gmail.com
    -
    Support: neutron050322@gmail.com
    -
    Unity Minor Version: 2021.3 LTS
    -
    License: Open Source (MIT)
    ===========================================================*/

/*===========================================================
    Atom Socket is a core library for the Atom framework, this was written with extreme performance in mind. 
    All classes are designed with zero memory allocations(or as low as possible) for the best performance, We have low-level tier allocations that we have no control over, ex: ReceiveFrom_icall(Mono) and FastAllocateString(Kernel OS)
    UDP Socket based communication is used to communicate with the remote host.
    The UDP protocol has three main parts: the header, the data.
    The header includes the following information: the channel, the target, the operation, and id of the sender.
    The channels are: Realible, Unreliable, and ReliableSequenced.
    -
    Realible are messages that are guaranteed to be received by the remote host.
    Unreliable are messages that are not guaranteed to be received by the remote host.
    ReliableSequenced are messages that are guaranteed to be received by the remote host, but are also guaranteed to be received in the same order as they were sent.
    -
    The target is the target of the message, ex: the target of the message is the server, client, or a specific object, etc.
    Target is client: the message is sent to the client.
    Target is server: the message is sent to the server.
    Target is object: the message is sent to the object.
    -   
    The operations can be the following: Sequence, Data, Ack.
    Sequence is used to send a message with a sequence number.
    Data is used to join the sequence number and the data on the same message(Client-Side), on the server side the data and the sequence number are separated into different messages.
    Acknowledgement is the message sequence number that the remote host received, used to acknowledge the message.

    Thanks (:

    ===========================================================*/

#if UNITY_2021_3_OR_NEWER
using Atom.Core.Wrappers;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using ThreadPriority = System.Threading.ThreadPriority;

namespace Atom.Core
{
    public class AtomSocket : MonoBehaviour
    {
        internal class AtomClient
        {
            internal AtomClient(ushort id, EndPoint endPoint)
            {
                IPEndPoint ipEndPoint = endPoint as IPEndPoint;
                Id = id;
                EndPoint = new AtomEndPoint(ipEndPoint.Address, ipEndPoint.Port);
            }

            public ushort Id { get; set; }
            public EndPoint EndPoint { get; set; }
        }

        private static readonly Channel[] _channelModes =
        {
             Channel.Unreliable,
             Channel.Reliable,
             Channel.ReliableAndOrderly
         };

        /// <summary> Id of connection, used to identify the connection.</summary>
        private ushort _id = 0;
        /// <summary>
        /// The socket used to receive and send the data, all data are received and sent simultaneously.
        /// Synchronous receive and send operations are used to avoid the overhead of asynchronous operations, 
        /// Unity doesn't like asynchronous operations, high CPU usage and a lot of garbage collection and low number of packets per second.
        /// And it doesn't matter if it's on a different thread, and because of that TCP is not welcome here...
        /// Only UDP is welcome here, so this socket implements three channels: unreliable, reliable and reliable ordered.
        /// Udp is connectionless, so we don't need async receive and send operations and we don't need extra threads either and we lighten the load of the garbage collector.
        /// This makes it a lot faster than TCP and perfect for Unity.
        /// </summary>
        private Socket _socket;
        /// <summary>
        /// The endpoint used to send data to the remote host, client only.
        /// I made a wrapper for this because a lot of garbage will be created if we use the IPEndPoint directly.
        /// </summary>
        private EndPoint _destEndPoint;
        /// <summary>
        /// Used to cancel the receive and send operations, called when the socket is closed.
        /// Prevents the CPU from spinning, Thread.Abort() is not recommended, because it's not a good way to stop a thread and not work on Linux OS.
        /// </summary>
        private CancellationTokenSource _cancelTokenSource;
        /// <summary>The list to store the connected clients. </summary>
        private readonly ConcurrentDictionary<EndPoint, AtomClient> ClientsByEndPoint = new();
        private readonly ConcurrentDictionary<ushort, AtomClient> ClientsById = new();
        /// <summary>
        /// Store the information of the channels.
        /// Ex: SentSequence, RecvSequence, Acknowledge....etc
        /// UDP sequence(seq) and acknowledgment(ack) numbers are used to detect lost packets and to detect packet reordering.
        /// The sequence number is incremented every time a packet is sent, and the acknowledgment number is incremented every time a packet is received.
        /// The acknowledgment number is used to confirm that the packet has been received, if not, the packet is resent.
        /// The sequence number is used to reorder packets, if the packet is out of order, the packet is reordered.
        /// </summary>
        private readonly ConcurrentDictionary<(ushort, byte), AtomChannel> ChannelsData = new();
        /// <summary>List of exlusive id's, used to prevent the same id to be used twice.</summary>
        private readonly AtomSafelyQueue<ushort> _ids = new(true);
        /// <summary>
        /// Returns whether the "Instance" is the Server or the Client.
        /// </summary>
        public bool IsServer { get; private set; }

#pragma warning disable IDE1006 // Estilos de Nomenclatura
        private void __Constructor__(EndPoint endPoint)
#pragma warning restore IDE1006 // Estilos de Nomenclatura
        {
            _socket = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
            {
                // The ReceiveBufferSize property gets or sets the number of bytes that you are expecting to store in the receive buffer for each read operation. 
                // This property actually manipulates the network buffer space allocated for receiving incoming data.
                ReceiveBufferSize = AtomGlobal.MaxRecBuffer,
                // The SendBufferSize property gets or sets the number of bytes that you are expecting to store in the send buffer for each send operation.
                // This property actually manipulates the network buffer space allocated for sending outgoing data.
                SendBufferSize = AtomGlobal.MaxSendBuffer,
            };
            _socket.Bind(endPoint);
            _cancelTokenSource = new();
            // Add the availables id's to the list.
            // This list is used to prevent the same id to be used twice.
            for (ushort i = 1; i < ushort.MaxValue; i++)
                _ids.Push(i, false);
            _ids.Sort();
        }

        protected void Initialize(EndPoint endPoint, bool isServer)
        {
            IsServer = isServer;
            // Initialize the constructor!
            __Constructor__(endPoint);
            // Start the receive thread.
            // The Unity API doesn't allow to be called from a thread other than the main thread.
            // The Unity API wil be dispatched to the main thread.
            // Why don't receive the data in the main thread?
            // Because the ReceiveFrom() method is blocking, FPS will be affected.
            // The Unity will be frozen until the data is received, but's not a good idead, right?
            Receive();
        }

        protected IEnumerator Connect(EndPoint endPoint)
        {
            IPEndPoint ipEndPoint = endPoint as IPEndPoint;
            _destEndPoint = new AtomEndPoint(ipEndPoint.Address, ipEndPoint.Port);
            while (true)
            {
                using (AtomStream message = new())
                {
                    message.Write((byte)Message.ConnectAndPing);
                    // The first packet is used to establish the connection.
                    // We are using unrealible channel, because we don't have and exclusive Id for the connection.
                    // We need an id to identify the connection, and the id is the "symbolic link" for the EndPoint...
                    // As we are using an unrealible channel, we need to send connection packets until we get a response.
                    SendToServer(message, Channel.Unreliable, Target.Single);
                }
                yield return new WaitForSeconds(10f);
            }
        }

        private void Relay(ushort playerId)
        {
            //for (int iCM = 0; iCM < _channelModes.Length; iCM++)
            //{
            //    Channel channelMode = _channelModes[iCM];
            //    if (ChannelsData.TryGetValue((playerId, (byte)channelMode), out AtomChannel channelData))
            //    {
            //        var packetsToRelay = channelData.MessagesToRelay.ToList();
            //        for (int i = 0; i < packetsToRelay.Count; i++)
            //        {
            //            var packetToRelay = packetsToRelay[i];
            //            AtomRelayMessage transmissionPacket = packetToRelay.Value;
            //            // Calc the last time we sent the packet.
            //            TimeSpan currentTime = DateTime.UtcNow.Subtract(transmissionPacket.LastSent);
            //            // If the time elapsed is greater than X second, the packet is re-sent if the packet is not acknowledged.
            //            if (currentTime.TotalSeconds >= /*ping time +*/ 0d) // formula: ping time + relay time, ping time is automatically compensated and added as the check is done on every ping packet.
            //            {
            //                Debug.LogError($"[Neutron] -> Re-try to send packet {packetToRelay.Key} -> : {transmissionPacket.SeqAck.ToString()} -> {packetToRelay.Value.Data.ChannelMode}");

            //                // if (!pKvP.Value.PacketsToReTransmit.ContainsKey(transmissionPacket.SeqAck))
            //                //     LogHelper.Error($"Re-transmit packet {pKvP.Key} : {transmissionPacket.SeqAck} not found.");
            //                // else
            //                //     LogHelper.Error($"Re-transmit packet {pKvP.Key} : {transmissionPacket.SeqAck} found.");

            //                (int, ushort) PTTKey = (transmissionPacket.SeqAck, transmissionPacket.Data.PlayerId);
            //                if (transmissionPacket.IsDisconnected())
            //                    channelData.PacketsToReTransmit.Remove(PTTKey, out _);
            //                else
            //                    Enqueue(transmissionPacket.Data);
            //                // Set the last time to current time when the packet is sent.
            //                transmissionPacket.LastSent = DateTime.UtcNow;
            //            }
            //        }
            //    }
            //    else
            //        LogHelper.Error($"ChannelData not found for playerId: {playerId} and channelMode: {channelMode}");
            //}
        }

        private void OnMessageCompleted(AtomStream reader, AtomStream writer, ushort playerId, EndPoint endPoint, Channel channelMode, Target targetMode, Operation opMode)
        {
            if (IsServer)
                OnServerMessageCompleted(reader, writer, playerId, endPoint, channelMode, targetMode, opMode, this);
            else
                OnClientMessageCompleted(reader, writer, playerId, endPoint, channelMode, targetMode, opMode, this);
        }

        protected virtual Message OnServerMessageCompleted(AtomStream reader, AtomStream writer, ushort playerId, EndPoint endPoint, Channel channelMode, Target targetMode, Operation opMode, AtomSocket udp)
        {
            reader.Read(out byte value);
            Message message = (Message)value;
            switch (message)
            {
                case Message.ConnectAndPing:
                    {
                        if (playerId == 0)
                        {
                            if (ClientsByEndPoint.ContainsKey(endPoint))
                            {
                                if (ClientsByEndPoint.TryRemove(endPoint, out AtomClient socketClient))
                                {
                                    if (ClientsById.TryRemove(socketClient.Id, out _))
                                    {
                                        RemoveChannel(socketClient.Id);
                                        ReturnId(socketClient.Id);
                                    }
                                    else
                                        Debug.LogError($"Client {socketClient.Id} not found in ClientsById.");
                                }
                                else
                                    Debug.LogError($"Failed to remove client {socketClient.Id} from the list.");
                            }

                            if (GetAvailableId(out ushort id))
                            {
                                AtomClient client = new(id, endPoint);
                                if (ClientsByEndPoint.TryAdd(endPoint, client))
                                {
                                    if (ClientsById.TryAdd(id, client))
                                    {
                                        AddChannel(id);
                                        writer.Write((byte)Message.ConnectAndPing);
                                        writer.Write(id);
                                        udp.SendToClient(writer, channelMode, targetMode, opMode, playerId, endPoint);
                                    }
                                    else
                                        Debug.LogError("Client not added!");
                                }
                                else
                                    Debug.LogError("Client not added!");
                            }
                            else
                                Debug.LogError("No available id's!");
                        }
                        else
                        {
                            Relay(playerId);
                            writer.Write((byte)Message.ConnectAndPing);
                            udp.SendToClient(writer, channelMode, targetMode, opMode, playerId, endPoint);
                        }
                    }
                    break;
                default:
                    return message;
            }
            return default;
        }

        protected virtual Message OnClientMessageCompleted(AtomStream reader, AtomStream writer, ushort playerId, EndPoint endPoint, Channel channelMode, Target targetMode, Operation opMode, AtomSocket udp)
        {
            reader.Read(out byte value);
            Message message = (Message)value;
            switch (message)
            {
                case Message.ConnectAndPing:
                    if (_id == 0)
                    {
                        _id.Read(reader);
                        for (ushort i = 0; i < ushort.MaxValue; i++)
                            AddChannel(i);
                    }
                    else
                        Relay(playerId);  // Let's relay the packets to the server.
                    break;
                default:
                    return message; // If not an private packet, return the packet type to process it.
            }
            // If is a private packet, return null to ignore it.
            return default;
        }

        private void AddChannel(ushort playerId)
        {
            for (int i = 0; i < _channelModes.Length; i++)
            {
                Channel channelMode = _channelModes[i];
                if (!ChannelsData.TryAdd((playerId, (byte)channelMode), new AtomChannel()))
                    Debug.LogError($"Channel {channelMode} already exists!");
            }
        }

        private void RemoveChannel(ushort playerId)
        {
            for (int i = 0; i < _channelModes.Length; i++)
            {
                Channel channelMode = _channelModes[i];
                if (!ChannelsData.TryRemove((playerId, (byte)channelMode), out _))
                    Debug.LogError($"Channel {channelMode} not found!");
            }
        }

        private void Relay(AtomMessage udpPacket, EndPoint endPoint, ushort playerId)
        {
            //if (!udpPacket.IsRTS)
            //{
            //    if (udpPacket.ChannelMode == ChannelMode.Reliable || udpPacket.ChannelMode == ChannelMode.ReliableSequenced)
            //    {
            //        // Don't create the transmission packet in Ack packets. 
            //        // because we are retransmitting the packet to the owner of the packet, in this case,
            //        // who takes care of the retransmission is the owner of the packet, 
            //        // so we can't re-transmit the packet because it already does that, otherwise it's in a retransmission loop.
            //        // Only clients who do not own(owner) the packet can re-transmit the packet.
            //        if (udpPacket.OperationMode == OperationMode.Acknowledgement)
            //            return;

            //        UdpPacket rtsUdpPacket = new UdpPacket(udpPacket.SeqAck, udpPacket.PlayerId, udpPacket.LastSent, endPoint, udpPacket.OperationMode, TargetMode.Single, udpPacket.ChannelMode, udpPacket.ChannelData, udpPacket.Data);
            //        rtsUdpPacket.IsRTS = true;
            //        // If channelData contains the packet, it means that the packet was lost, and we need to re-transmit it.
            //        (int, ushort) PTTKey = (rtsUdpPacket.SeqAck, playerId);
            //        if (!rtsUdpPacket.ChannelData.PacketsToReTransmit.ContainsKey(PTTKey))
            //            rtsUdpPacket.ChannelData.PacketsToReTransmit.TryAdd(PTTKey, new TransmissionPacket(rtsUdpPacket.SeqAck, DateTime.UtcNow, rtsUdpPacket));
            //        else
            //        {
            //            if (rtsUdpPacket.ChannelData.PacketsToReTransmit[PTTKey].IsDisconnected())
            //                rtsUdpPacket.ChannelData.PacketsToReTransmit.Remove(PTTKey, out _);
            //        }
            //    }
            //}
            //else
            //{ /*Prevent duplicate RTS Packet*/ }
        }

        protected void SendToClient(AtomStream message, Channel channel, Target targetMode, Operation opMode, ushort playerId, EndPoint endPoint)
        {
            if (opMode == Operation.Sequence)
                SendToClient(message, channel, targetMode, playerId, endPoint);
        }

        protected void SendToServer(AtomStream message, Channel channel, Target targetMode)
        {
#if ATOM_DEBUG
            if (((channel == Channel.ReliableAndOrderly || channel == Channel.Reliable) && _id == 0) || _destEndPoint == null)
                throw new Exception("[Atom] -> You must connect to the server before sending data.");
#endif
            Send(message, channel, targetMode, Operation.Sequence, _id, _destEndPoint, 0);
        }

        private void SendToClient(AtomStream message, Channel channel, Target targetMode, ushort playerId, EndPoint endPoint)
        {
            Send(message, channel, targetMode, Operation.Data, playerId, endPoint, 0);
        }

        private void Send(AtomStream messageStream, Channel channelMode, Target targetMode, Operation opMode, ushort playerId, EndPoint endPoint, int seqAck = 0)
        {
            var data = messageStream.GetBufferAsReadOnlySpan();
            int defSize = (channelMode == Channel.ReliableAndOrderly || channelMode == Channel.Reliable) ? AtomCore.RealibleSize : AtomCore.UnrealibleSize;
#if ATOM_DEBUG
            if (messageStream.FixedSize)
            {
                if ((data.Length + defSize) != messageStream.Size)
                    throw new Exception("[Atom] -> The size of the packet is not correct. You setted " + messageStream.Size + " but you need " + (data.Length + defSize) + ".");
            }
#endif
            // data >> header
            messageStream.Reset(pos: defSize);
            messageStream.Write(data);
            messageStream.Position = 0;
            // header << data
            messageStream.Write((byte)channelMode);
            messageStream.Write((byte)targetMode);
            messageStream.Write((byte)opMode);
            messageStream.Write(playerId);
            // Move the position to the end, message is fully written.
            messageStream.Position = messageStream.CountBytes;
            using (AtomMessage _message = AtomMessage.Get())
            {
                if (channelMode == Channel.Reliable || channelMode == Channel.ReliableAndOrderly)
                {
                    AtomChannel channelData = ChannelsData[(playerId, (byte)channelMode)];
                    if (seqAck == 0)
                        seqAck = Interlocked.Increment(ref channelData.SentAck);
                    // Write the sequence number in the header!
                    messageStream.Write(seqAck);
                    // Write the realible message.
                    _message.SeqAck = seqAck;
                    _message.PlayerId = playerId;
                    _message.LastSent = DateTime.UtcNow;
                    _message.EndPoint = endPoint;
                    _message.Operation = opMode;
                    _message.Target = targetMode;
                    _message.Channel = channelMode;
                    _message.AtomChannel = channelData;
                    _message.Data = messageStream.GetBufferAsCopy();
                }
                else if (channelMode == Channel.Unreliable)
                {
                    _message.PlayerId = playerId;
                    _message.EndPoint = endPoint;
                    _message.Operation = opMode;
                    _message.Target = targetMode;
                    _message.Channel = channelMode;
                    _message.Data = messageStream.GetBufferAsCopy();
                }
                Send(_message);
            }
        }

        private void Send(AtomMessage message)
        {
            if (IsServer)
            {
                //! "CreateTransmissionPacket(udpPacket)" must be called before _socket.SendTo(udpPacket.Data, udpPacket.EndPoint), otherwise the packet will be lost.
                //! Sometimes the Ack will arrive before the transmission packet is created, and the packet will be lost, so we need to create the transmission packet before sending the packet.
                //! "isConnected" is false, is server, so we need to send the data to the client or clients.
                switch (message.Target)
                {
                    case Target.All:
                        Relay(message, message.EndPoint, message.PlayerId);
                        _socket.SendTo(message.Data, message.EndPoint);
                        foreach (var KvP in ClientsByEndPoint.ToList())
                        {
                            if (!KvP.Key.Equals(message.EndPoint))
                            {
                                AtomClient socketClient = KvP.Value;
                                Relay(message, socketClient.EndPoint, socketClient.Id);
                                _socket.SendTo(message.Data, socketClient.EndPoint);
                            }
                            else
                                continue;
                        }
                        break;
                    case Target.Others:
                        // // If the packet is sent to others, it's necessary to send it to all the clients except the sender.
                        // // The sender is the owner of the packet, so we don't need to send it to the sender.
                        // foreach (var KvP in ClientsByEndPoint.ToList())
                        // {
                        //     if (!KvP.Key.Equals(udpPacket.EndPoint))
                        //     {
                        //         SocketClient socketClient = KvP.Value;
                        //         ChannelData channeldata = ChannelsData[(socketClient.Id, (byte)udpPacket.ChannelMode)];
                        //         CreateTransmissionPacket(udpPacket, channeldata, socketClient.EndPoint);
                        //         _socket.SendTo(udpPacket.Data, KvP.Key);
                        //     }
                        //     else
                        //         continue;
                        // }
                        break;
                    case Target.Single:
                        Relay(message, message.EndPoint, message.PlayerId);
                        _socket.SendTo(message.Data, message.EndPoint);
                        break;
                }
            }
            else
            {
                Relay(message, message.EndPoint, message.PlayerId);
                _socket.SendTo(message.Data, message.EndPoint);
            }
        }

        private void Receive()
        {
            new Thread(() =>
            {
                try
                {
                    // Bandwidth Control for incoming data.
                    AtomBandwidth bandwidthCounter = new();
                    // The endpoint used store the address of the remote host.
                    // I made a wrapper for this because a lot of garbage will be created if we use the IPEndPoint directly.
                    // Note that: the client must send something to the server first(establish a connection), otherwise, the directly send from the server to the client will fail, this is called a "Handshake".
                    // To P2P, the client send a packet to the server, and the server send a packet to the client(establish a connection), Now the client's router allows the remote host to send packets to the client and vice versa.
                    // Let's get the address and port of the client and send to the others clients, others clients will send to this address and port, this iw how P2P works.
                    // Remember that we need the server to keep sending packets to the client(Keep Alive) and vice versa, otherwise, the connection will be lost.
                    // This technique is known as "UDP Hole Punching".
                    EndPoint _peerEndPoint = new AtomEndPoint(IPAddress.Any, 0);
                    // Create a buffer to receive the data.
                    // The size of the buffer is the maximum size of the data that can be received(MTU Size).
                    byte[] buffer = new byte[1536];
                    // A memory block to store the received data.
                    // Prevent a new allocation every time we receive data and the copy of the data.
                    ReadOnlySpan<byte> _buffer = buffer;
                    while (!_cancelTokenSource.IsCancellationRequested)
                    {
                        bandwidthCounter.Start();
                        int bytesTransferred = _socket.ReceiveFrom(buffer, ref _peerEndPoint);
                        if (bytesTransferred >= 0)
                        {
                            bandwidthCounter.Stop();
                            bandwidthCounter.Add(bytesTransferred);
                            bandwidthCounter.Get(out int bytesRate, out int messageRate);
#if UNITY_SERVER
                            if (bytesRate > 0 && messageRate > 0)
                                Console.WriteLine($"Avg: Rec {bytesTransferred} bytes, {bytesRate} bytes/s, {messageRate} messages/s");
#else
                            if (bytesRate > 0 && messageRate > 0)
                                Debug.LogFormat(LogType.Error, LogOption.NoStacktrace, null, "Avg: Rec {0} bytes | {1} bytes/s | {2} messages/s", bytesTransferred, bytesRate, messageRate);
#endif
                            using (AtomStream atomStream = AtomStream.Get())
                            {
                                atomStream.SetBuffer(_buffer[..bytesTransferred]);
                                atomStream.Read(out byte channelByte);
                                atomStream.Read(out byte targetByte);
                                atomStream.Read(out byte opByte);
                                atomStream.Read(out ushort playerId);
                                // Parse bytes.....
                                Channel channelMode = (Channel)channelByte;
                                Target targetMode = (Target)targetByte;
                                Operation opMode = (Operation)opByte;
                                if (channelMode == Channel.Reliable || channelMode == Channel.ReliableAndOrderly)
                                {
                                    (ushort, byte) chKey = (playerId, (byte)channelMode);
                                    atomStream.Read(out int seqAck);
                                    if (opMode == Operation.Acknowledgement)
                                    {
                                        if (!IsServer)
                                            ChannelsData[chKey].MessagesToRelay.TryRemove((seqAck, playerId), out _);
                                        else
                                        {
                                            AtomClient peer = ClientsById[playerId];
                                            AtomClient otherPeer = ClientsByEndPoint[_peerEndPoint];
                                            bool isOtherPeer = !peer.EndPoint.Equals(_peerEndPoint);
                                            ushort peerId = isOtherPeer ? otherPeer.Id : playerId;
                                            ChannelsData[chKey].MessagesToRelay.TryRemove((seqAck, peerId), out _);
                                        }
                                    }
                                    else
                                    {
                                        Send(AtomStream.None, channelMode, Target.Single, Operation.Acknowledgement, playerId, _peerEndPoint, seqAck);
                                        ReadOnlySpan<byte> data = atomStream.ReadNext();
                                        if (channelMode == Channel.Reliable)
                                        {
                                            if (!ChannelsData[chKey].Acknowledgements.TryAdd(seqAck, seqAck))
                                                continue;

                                            using (AtomStream reader = AtomStream.Get())
                                            {
                                                using (AtomStream writer = AtomStream.Get())
                                                {
                                                    reader.SetBuffer(data);
                                                    OnMessageCompleted(reader, writer, playerId, _peerEndPoint, channelMode, targetMode, opMode);
                                                }
                                            }
                                        }
                                        else if (channelMode == Channel.ReliableAndOrderly)
                                        {
                                            if (seqAck <= ChannelsData[chKey].LastProcessedSequentialAck)
                                                continue;

                                            if (ChannelsData[chKey].SequentialData.ContainsKey(seqAck))
                                            {
                                                ChannelsData[chKey].SequentialData.Add(seqAck, data.ToArray());
                                                if (ChannelsData[chKey].IsSequential())
                                                {
                                                    var KvPSenquentialData = ChannelsData[chKey].SequentialData.ToList();
                                                    for (int i = 0; i < KvPSenquentialData.Count; i++)
                                                    {
                                                        var KvP = KvPSenquentialData[i];
                                                        if (KvP.Key > ChannelsData[chKey].LastProcessedSequentialAck)
                                                        {
                                                            using (AtomStream reader = AtomStream.Get())
                                                            {
                                                                using (AtomStream writer = AtomStream.Get())
                                                                {
                                                                    reader.SetBuffer(KvP.Value);
                                                                    OnMessageCompleted(reader, writer, playerId, _peerEndPoint, channelMode, targetMode, opMode);
                                                                }
                                                            }
                                                            ChannelsData[chKey].LastProcessedSequentialAck++;
                                                        }
                                                        else
                                                            ChannelsData[chKey].SequentialData.Remove(KvP.Key);
                                                    }
                                                    ChannelsData[chKey].LastReceivedSequentialAck = KvPSenquentialData.Last().Key;
                                                }
                                                else {/*Waiting for more packets, to create the sequence*/}
                                            }
                                            else {/*Discard duplicate packet....*/}
                                        }
                                    }
                                }
                                else if (channelMode == Channel.Unreliable)
                                {
                                    ReadOnlySpan<byte> data = atomStream.ReadNext();
                                    using (AtomStream reader = AtomStream.Get())
                                    {
                                        using (AtomStream writer = AtomStream.Get())
                                        {
                                            reader.SetBuffer(data);
                                            OnMessageCompleted(reader, writer, playerId, _peerEndPoint, channelMode, targetMode, opMode);
                                        }
                                    }
                                }
                            }
                        }
                        else
                            Debug.LogError("\r\nReceiveFrom() failed with error code: " + bytesTransferred);
                    }
                }
                catch (SocketException ex)
                {
                    if (ex.ErrorCode == 10004)
                        return;
                    Debug.LogException(ex);
                }
                catch (ThreadAbortException) { }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            })
            {
                Name = "Atom_RecvThread",
                Priority = ThreadPriority.Highest,
                IsBackground = true,
            }.Start();
        }

        private bool GetAvailableId(out ushort id)
        {
            id = 0;
            if (_ids.Count > 0)
                id = _ids.Pull();
            return id != 0;
        }

        private void ReturnId(ushort id) => _ids.Push(id, true);
        public Socket GetSocket() => _socket;
        protected void Close()
        {
            try
            {
                _socket.Close();
                _cancelTokenSource.Cancel();
            }
            finally
            {
                _cancelTokenSource.Dispose();
            }
        }
    }
}
#endif