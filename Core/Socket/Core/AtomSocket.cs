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
    Neutron Core is a core library for the Atom framework.
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
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Atom.Core
{
    public class AtomSocket
    {
        public class AtomClient
        {
            public AtomClient(ushort id, EndPoint endPoint)
            {
                Id = id;
                EndPoint = endPoint;
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
        /// The Connected property gets the connection state of the Socket as of the last I/O operation. 
        /// When it returns false, the Socket was either never connected, or is no longer connected
        /// </summary>
        private bool _isConnected;
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
        /// <summary>The endpoint used to listen for data from the remote host.</summary>
        private EndPoint _srcEndPoint;
        /// <summary>
        /// Used to cancel the receive and send operations, called when the socket is closed.
        /// Prevents the CPU from spinning, Thread.Abort() is not recommended, because it's not a good way to stop a thread and not work on Linux OS.
        /// </summary>
        private CancellationTokenSource _cancelTokenSource;
        /// <summary> Used to enqueue the received data, the data is enqueued in a queue, and the queue is processed in a thread.</summary>
        private BlockingCollection<AtomMessage> _dataToSend;
        /// <summary>The list to store the connected clients. </summary>
        internal ConcurrentDictionary<EndPoint, AtomClient> ClientsByEndPoint = new();
        internal ConcurrentDictionary<ushort, AtomClient> ClientsById = new();
        /// <summary>
        /// Store the information of the channels.
        /// Ex: SentSequence, RecvSequence, Acknowledge....etc
        /// UDP sequence(seq) and acknowledgment(ack) numbers are used to detect lost packets and to detect packet reordering.
        /// The sequence number is incremented every time a packet is sent, and the acknowledgment number is incremented every time a packet is received.
        /// The acknowledgment number is used to confirm that the packet has been received, if not, the packet is resent.
        /// The sequence number is used to reorder packets, if the packet is out of order, the packet is reordered.
        /// </summary>
        internal ConcurrentDictionary<(ushort, byte), AtomChannel> ChannelsData = new();
        /// <summary>List of exlusive id's, used to prevent the same id to be used twice.</summary>
        private AtomSafelyQueue<ushort> _ids = new(true);
        /// <summary>
        /// Returns whether the "Instance" is the Server or the Client.
        /// </summary>
        public bool IsServer => !_isConnected;

        /// <summary>
        /// Associates a Socket with a local endpoint.
        /// </summary>
        public void Bind(EndPoint endPoint)
        {
            _cancelTokenSource = new();
            _dataToSend = new();
            _socket = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
            {
                // The ReceiveBufferSize property gets or sets the number of bytes that you are expecting to store in the receive buffer for each read operation. 
                // This property actually manipulates the network buffer space allocated for receiving incoming data.
                ReceiveBufferSize = 8192,
                // The SendBufferSize property gets or sets the number of bytes that you are expecting to store in the send buffer for each send operation.
                // This property actually manipulates the network buffer space allocated for sending outgoing data.
                SendBufferSize = 8192,
            };
            // Bind the socket to the endpoint.
            // This address and port will be used to receive data from the remote host.
            _socket.Bind(endPoint);
            // The LocalEndPoint property gets the local endpoint of the socket.
            _srcEndPoint = endPoint;
            // Add the availables id's to the list.
            // This list is used to prevent the same id to be used twice.
            for (ushort i = 1; i < ushort.MaxValue; i++)
                _ids.Push(i, false);
            _ids.Sort();
        }

        /// <summary>
        /// Initialize the send and receive channels.
        /// </summary>
        public void InitThreads()
        {
            // Start the receive thread.
            // The Unity API doesn't allow to be called from a thread other than the main thread.
            // The Unity API wil be dispatched to the main thread.
            // Why don't receive the data in the main thread?
            // Because the ReceiveFrom() method is blocking, FPS will be affected.
            // The Unity will be frozen until the data is received, but's not a good idead, right?
            InitRecThread();
            // Start the send thread.
            // This thread is used to send data to the remote host.
            // Why don't we send the data directly from the receive thread or Unity's main thread?
            // Because the send method is blocking, and we don't want to block Unity's main thread, FPS will be affected.
            // Let's the data to a queue, and the queue is processed in a thread.
            InitSentThread();
        }

        /// <summary>
        /// Connect to the remote host.
        /// </summary>
        public IEnumerator Connect(EndPoint endPoint)
        {
            while (true)
            {
                // The endpoint used to send data to the remote host, client only.
                _destEndPoint = endPoint;
                using (NeutronStream packet = Neutron.PooledNetworkStreams.Pull())
                {
                    var writer = packet.Writer;
                    writer.WritePacket((byte)PacketType.ConnectAndPing);
                    // The first packet is used to establish the connection.
                    // We are using unrealible channel, because we don't have and exclusive Id for the connection.
                    // We need an id to identify the connection, and the id is the "symbolic link" for the EndPoint...
                    // As we are using an unrealible channel, we need to send connection packets until we get a response.
                    SendToServer(packet, ChannelMode.Unreliable, TargetMode.Single);
                }
                // Wait for the response and ping if connected...
                yield return new WaitForSeconds(0.2f);
            }
        }

        /// <summary>
        /// Relay the data to the remote host.
        /// </summary>
        private void Relay(ushort playerId)
        {
            for (int iCM = 0; iCM < _channelModes.Length; iCM++)
            {
                ChannelMode channelMode = _channelModes[iCM];
                if (ChannelsData.TryGetValue((playerId, (byte)channelMode), out ChannelData channelData))
                {
                    var packetsToRelay = channelData.PacketsToReTransmit.ToList();
                    for (int i = 0; i < packetsToRelay.Count; i++)
                    {
                        var packetToRelay = packetsToRelay[i];
                        TransmissionPacket transmissionPacket = packetToRelay.Value;
                        // Calc the last time we sent the packet.
                        TimeSpan currentTime = DateTime.UtcNow.Subtract(transmissionPacket.LastSent);
                        // If the time elapsed is greater than X second, the packet is re-sent if the packet is not acknowledged.
                        if (currentTime.TotalSeconds >= /*ping time +*/ 0d) // formula: ping time + relay time, ping time is automatically compensated and added as the check is done on every ping packet.
                        {
                            LogHelper.Error($"[Neutron] -> Re-try to send packet {packetToRelay.Key} -> : {transmissionPacket.SeqAck.ToString()} -> {packetToRelay.Value.Data.ChannelMode}");

                            // if (!pKvP.Value.PacketsToReTransmit.ContainsKey(transmissionPacket.SeqAck))
                            //     LogHelper.Error($"Re-transmit packet {pKvP.Key} : {transmissionPacket.SeqAck} not found.");
                            // else
                            //     LogHelper.Error($"Re-transmit packet {pKvP.Key} : {transmissionPacket.SeqAck} found.");

                            (int, ushort) PTTKey = (transmissionPacket.SeqAck, transmissionPacket.Data.PlayerId);
                            if (transmissionPacket.IsDisconnected())
                                channelData.PacketsToReTransmit.Remove(PTTKey, out _);
                            else
                                Enqueue(transmissionPacket.Data);
                            // Set the last time to current time when the packet is sent.
                            transmissionPacket.LastSent = DateTime.UtcNow;
                        }
                    }
                }
                else
                    LogHelper.Error($"ChannelData not found for playerId: {playerId} and channelMode: {channelMode}");
            }
        }

        /// <summary>
        /// Process the internal packet queue.
        /// </summary>
        internal PacketType OnServerMessageCompleted(NeutronStream stream, ushort playerId, EndPoint endPoint, ChannelMode channelMode, TargetMode targetMode, OperationMode opMode, AtomSocket udp)
        {
            var reader = stream.Reader;
            var writer = stream.Writer;
            // Convert byte to packet type.
            var packetType = (PacketType)reader.ReadByte();
            switch (packetType)
            {
                // Let's process the packet.
                case PacketType.ConnectAndPing:
                    {
                        if (playerId == 0)
                        {
                            if (ClientsByEndPoint.ContainsKey(endPoint))
                            {
                                if (ClientsByEndPoint.Remove(endPoint, out SocketClient socketClient))
                                {
                                    RemoveChannel(socketClient.Id);
                                    ReturnId(socketClient.Id);
                                }
                            }

                            if (GetAvailableId(out ushort id))
                            {
                                // Add the new client to server.
                                if (ClientsByEndPoint.TryAdd(endPoint, new SocketClient(id, endPoint)))
                                {
                                    LogHelper.Error($"Added a new player {id}");
                                    // Create the local channels to send and receive data.
                                    AddChannel(id);
                                    writer.WritePacket((byte)PacketType.ConnectAndPing);
                                    writer.Write(id);
                                    udp.SendToClient(stream, channelMode, targetMode, opMode, playerId, endPoint);
                                    // Add the new client to server.
                                }
                                else
                                    LogHelper.Error("Client not added!");
                            }
                            else
                                LogHelper.Error("[Neutron] -> No available id's.");
                        }
                        else
                        {
                            Relay(playerId);  // Let's relay the packets to the client.
                                              // Send the ping packet to the client.
                            writer.WritePacket((byte)PacketType.ConnectAndPing);
                            udp.SendToClient(stream, channelMode, targetMode, opMode, playerId, endPoint);
                        }
                    }
                    break;
                default:
                    return packetType; // If not an internal packet, return the packet type to process it.
            }
            // If is a internal packet, return null to ignore it.
            return default;
        }

        /// <summary>
        /// Process the internal packet queue.
        /// </summary>
        internal PacketType OnClientMessageCompleted(NeutronStream stream, ushort playerId, EndPoint endPoint, ChannelMode channelMode, TargetMode targetMode, OperationMode opMode, AtomSocket udp)
        {
            var reader = stream.Reader;
            var writer = stream.Writer;
            // Convert byte to packet type.
            var packetType = (PacketType)reader.ReadByte();
            switch (packetType)
            {
                case PacketType.ConnectAndPing:
                    if (_id == 0)
                    {
                        _id = reader.ReadUShort();
                        for (ushort i = 0; i < ushort.MaxValue; i++) // Create the local channels to send and receive data.
                            AddChannel(i); // One channel for each id.
                        _isConnected = true;
                    }
                    else
                        Relay(playerId);  // Let's relay the packets to the server.
                    break;
                default:
                    return packetType; // If not an internal packet, return the packet type to process it.
            }
            // If is a internal packet, return null to ignore it.
            return default;
        }

        private void AddChannel(ushort playerId)
        {
            for (int i = 0; i < _channelModes.Length; i++)
            {
                ChannelMode channelMode = _channelModes[i];
                if (!ChannelsData.TryAdd((playerId, (byte)channelMode), new ChannelData()))
                    LogHelper.Error($"Channel {channelMode} already exists!");
            }
        }

        private void RemoveChannel(ushort playerId)
        {
            for (int i = 0; i < _channelModes.Length; i++)
            {
                ChannelMode channelMode = _channelModes[i];
                if (!ChannelsData.Remove((playerId, (byte)channelMode), out _))
                    LogHelper.Error($"Channel {channelMode} not found!");
            }
        }

        private void ConfigureRetransmission(UdpPacket udpPacket, EndPoint endPoint, ushort playerId)
        {
            if (!udpPacket.IsRTS)
            {
                if (udpPacket.ChannelMode == ChannelMode.Reliable || udpPacket.ChannelMode == ChannelMode.ReliableSequenced)
                {
                    // Don't create the transmission packet in Ack packets. 
                    // because we are retransmitting the packet to the owner of the packet, in this case,
                    // who takes care of the retransmission is the owner of the packet, 
                    // so we can't re-transmit the packet because it already does that, otherwise it's in a retransmission loop.
                    // Only clients who do not own(owner) the packet can re-transmit the packet.
                    if (udpPacket.OperationMode == OperationMode.Acknowledgement)
                        return;

                    UdpPacket rtsUdpPacket = new UdpPacket(udpPacket.SeqAck, udpPacket.PlayerId, udpPacket.LastSent, endPoint, udpPacket.OperationMode, TargetMode.Single, udpPacket.ChannelMode, udpPacket.ChannelData, udpPacket.Data);
                    rtsUdpPacket.IsRTS = true;
                    // If channelData contains the packet, it means that the packet was lost, and we need to re-transmit it.
                    (int, ushort) PTTKey = (rtsUdpPacket.SeqAck, playerId);
                    if (!rtsUdpPacket.ChannelData.PacketsToReTransmit.ContainsKey(PTTKey))
                        rtsUdpPacket.ChannelData.PacketsToReTransmit.TryAdd(PTTKey, new TransmissionPacket(rtsUdpPacket.SeqAck, DateTime.UtcNow, rtsUdpPacket));
                    else
                    {
                        if (rtsUdpPacket.ChannelData.PacketsToReTransmit[PTTKey].IsDisconnected())
                            rtsUdpPacket.ChannelData.PacketsToReTransmit.Remove(PTTKey, out _);
                    }
                }
            }
            else
            { /*Prevent duplicate RTS Packet*/ }
        }

        /// <summary>
        /// Enqueue the data to the send queue.
        /// </summary>
        private void Enqueue(UdpPacket udpPacket)
        {
            // Enqueue data to send.
            // This operation is thread safe, it's necessary to lock the queue? Yes, because data can be enqueued from different threads,
            // example: Unity's main thread and the receive thread.
            _dataToSend.Push(udpPacket);
        }

        /// <summary>
        /// Send the data to the remote host. Server->Client.
        /// </summary>
        public void SendToClient(NeutronStream dataStream, ChannelMode channel, TargetMode targetMode, OperationMode opMode, ushort playerId, EndPoint endPoint)
        {
            if (opMode == OperationMode.Sequence)
                SendToClient(dataStream, channel, targetMode, playerId, endPoint);
        }

        /// <summary>
        /// Send the data to the remote host. Client->Server.
        /// </summary>
        public void SendToServer(NeutronStream dataStream, ChannelMode channel, TargetMode targetMode)
        {
            if ((channel == ChannelMode.ReliableSequenced || channel == ChannelMode.Reliable) && _id == 0)
                throw new Exception("[Neutron] -> You must connect to the server before sending data.");
            Send(dataStream, channel, targetMode, OperationMode.Sequence, _id, _destEndPoint, 0);
        }

        /// <summary>
        /// Send the data to the remote host. Server->Client.
        /// </summary>
        private void SendToClient(NeutronStream dataStream, ChannelMode channel, TargetMode targetMode, ushort playerId, EndPoint endPoint)
        {
            Send(dataStream, channel, targetMode, OperationMode.Data, playerId, endPoint, 0);
        }

        private void Send(NeutronStream dataStream, ChannelMode channelMode, TargetMode targetMode, OperationMode opMode, ushort playerId, EndPoint endPoint, int seqAck = 0)
        {
            // Get the data from the stream.
            // This data will be sent to the remote host.
            var data = dataStream.Writer.GetBufferAsReadOnlySpan();
            // Let's to create the packet.
            // The header includes the channel, the sequence number and the acknowledgment number for reliable channels.
            using (NeutronStream packet = Neutron.PooledNetworkStreams.Pull())
            {
                var writer = packet.Writer;
                writer.Write((byte)channelMode);
                writer.Write((byte)targetMode);
                writer.Write((byte)opMode);
                writer.Write(playerId);
                if (channelMode == ChannelMode.Reliable || channelMode == ChannelMode.ReliableSequenced)
                {
                    (ushort, byte) chKey = (playerId, (byte)channelMode);
                    ChannelData channelData = ChannelsData[chKey];
                    // If sequence is 0, the packet is sent as a new packet, otherwise it is a re-transmission.
                    if (seqAck == 0)
                        seqAck = Interlocked.Increment(ref channelData.SentAck);
                    // The client sends the sequence number(4 bytes(int)) and the data.
                    writer.Write(seqAck);
                    writer.Write(data);
                    // Create a copy of the packet.
                    byte[] pData = writer.GetBufferAsCopy();
                    // Create UdpPacket to send.
                    // The packet includes the data and the endpoint.
                    // The packet is sent to the remote host.
                    UdpPacket udpPacket = new UdpPacket(seqAck, playerId, DateTime.UtcNow, endPoint, opMode, targetMode, channelMode, channelData, pData);
                    // Send the reliable packet.
                    Enqueue(udpPacket);
                }
                else if (channelMode == ChannelMode.Unreliable)
                {
                    // Send the unreliable packet.
                    writer.Write(data);
                    UdpPacket udpPacket = new UdpPacket(playerId, endPoint, opMode, targetMode, channelMode, writer.GetBufferAsCopy());
                    Enqueue(udpPacket);
                }
            }
        }

        private void InitSentThread()
        {
            new Thread(() =>
            {
                // Let' send the data in a loop.
                while (!_cancelTokenSource.IsCancellationRequested)
                {
                    // Le't get the data from the queue and send it.
                    // This collection is blocked until the data is available, prevents de CPU from spinning.
                    UdpPacket udpPacket = _dataToSend.Pull();
                    if (IsServer)
                    {
                        //! "CreateTransmissionPacket(udpPacket)" must be called before _socket.SendTo(udpPacket.Data, udpPacket.EndPoint), otherwise the packet will be lost.
                        //! Sometimes the Ack will arrive before the transmission packet is created, and the packet will be lost, so we need to create the transmission packet before sending the packet.

                        //! "isConnected" is false, is server, so we need to send the data to the client or clients.
                        switch (udpPacket.TargetMode)
                        {
                            // Send and Create the transmission packet.
                            // This transmission packet is used to re-transmit the packet if the packet is lost.
                            // The packet is not created if the packet is an acknowledgment packet or is a duplicate packet.
                            case TargetMode.All:
                                ConfigureRetransmission(udpPacket, udpPacket.EndPoint, udpPacket.PlayerId);
                                _socket.SendTo(udpPacket.Data, udpPacket.EndPoint);
                                // Send the data to all the clients.
                                foreach (var KvP in ClientsByEndPoint.ToList())
                                {
                                    if (!KvP.Key.Equals(udpPacket.EndPoint))
                                    {
                                        SocketClient socketClient = KvP.Value;
                                        ConfigureRetransmission(udpPacket, socketClient.EndPoint, socketClient.Id);
                                        // Send the packet to the remote host.
                                        _socket.SendTo(udpPacket.Data, socketClient.EndPoint);
                                    }
                                    else
                                        continue;
                                }
                                break;
                            case TargetMode.Others:
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
                            case TargetMode.Single:
                                ConfigureRetransmission(udpPacket, udpPacket.EndPoint, udpPacket.PlayerId);
                                // Send the packet to the remote host.
                                _socket.SendTo(udpPacket.Data, udpPacket.EndPoint);
                                break;
                        }
                    }
                    else // "isConnected" is true, is client, so we need to send the data to the server.
                    {
                        ConfigureRetransmission(udpPacket, udpPacket.EndPoint, udpPacket.PlayerId);
                        // Send the packet to the remote host.
                        _socket.SendTo(udpPacket.Data, udpPacket.EndPoint);
                    }
                }
            })
            {
                Name = "Neutron_SentThread",
                Priority = ThreadPriority.Normal,
                IsBackground = true,
            }.Start();
        }

        private void InitRecThread()
        {
            new Thread(() =>
            {
                try
                {
                    // Bandwidth Control for incoming data.
                    BandwidthCounter bandwidthCounter = new BandwidthCounter();
                    // The endpoint used store the address of the remote host.
                    // I made a wrapper for this because a lot of garbage will be created if we use the IPEndPoint directly.
                    // Note that: the client must send something to the server first(establish a connection), otherwise, the directly send from the server to the client will fail, this is called a "Handshake".
                    // To P2P, the client send a packet to the server, and the server send a packet to the client(establish a connection), Now the client's router allows the remote host to send packets to the client and vice versa.
                    // Let's get the address and port of the client and send to the others clients, others clients will send to this address and port, this iw how P2P works.
                    // Remember that we need the server to keep sending packets to the client(Keep Alive) and vice versa, otherwise, the connection will be lost.
                    // This technique is known as "UDP Hole Punching".
                    EndPoint _peerEndPoint = new IPEndPoint(IPAddress.Any, 0);
                    // Create a buffer to receive the data.
                    // The size of the buffer is the maximum size of the data that can be received(MTU Size).
                    byte[] buffer = new byte[1536];
                    // A memory block to store the received data.
                    // Prevent a new allocation every time we receive data and the copy of the data.
                    Memory<byte> memoryBuffer = buffer;
                    // Start receiving data.
                    while (!_cancelTokenSource.IsCancellationRequested)
                    {
                        bandwidthCounter.Start();
                        int bytesTransferred = _socket.ReceiveFrom(buffer, ref _peerEndPoint);
                        if (bytesTransferred >= 0)
                        {
                            bandwidthCounter.Stop();
                            bandwidthCounter.Add(bytesTransferred);
                            bandwidthCounter.Get();
                            using (NeutronStream dataStream = Neutron.PooledNetworkStreams.Pull())
                            {
                                var writer = dataStream.Writer;
                                var reader = dataStream.Reader;
                                reader.SetBuffer(memoryBuffer[..bytesTransferred].Span);
                                // Get the header of Custom Protocol and create the MultiEndPoint
                                // MultiEndPoint is a tuple that contains the address of the remote host, the port and the channel.
                                // MultiEndPoint is used to separate the channels, each player has their own channel separating their data and properties.
                                ChannelMode channel = (ChannelMode)reader.ReadByte();
                                TargetMode targetMode = (TargetMode)reader.ReadByte();
                                OperationMode opMode = (OperationMode)reader.ReadByte();
                                ushort playerId = reader.ReadUShort();
                                // If the channel is reliable, let's read the sequence number.
                                if (channel == ChannelMode.Reliable || channel == ChannelMode.ReliableSequenced)
                                {
                                    (ushort, byte) chKey = (playerId, (byte)channel);
                                    // The acknowledgement is the first 4 bytes of the packet.
                                    // Let's send it to the remote host to confirm that we received the packet.
                                    int seqAck = reader.ReadInt();
                                    // If the packet was confirmed, let's remove it from the list of packets to re-transmit.
                                    // After that, let's send the data to the remote host.
                                    if (opMode == OperationMode.Acknowledgement)
                                    {
                                        if (!IsServer)
                                        {
                                            ChannelsData[chKey].PacketsToReTransmit.Remove((seqAck, playerId), out _);
                                            //ChannelsData[chKey].PacketsToReTransmit.Remove(seqAck, out _);
                                            // LogHelper.Error($"Removing packet -> {seqAck} : {playerId} : {channel}");
                                            // if (!ChannelsData[chKey].PacketsToReTransmit.Remove((seqAck, playerId), out _))
                                            //     LogHelper.Error($"The packet with sequence number {seqAck} was not found in the list of packets to re-transmit -> {(ChannelsData[chKey].PacketsToReTransmit.ContainsKey((seqAck, playerId))).ToString()}");
                                            // else
                                            //     LogHelper.Error($"The packet with sequence number {seqAck} was removed from the list of packets to re-transmit.");
                                        }
                                        else
                                        {
                                            // Find the original peer of packet
                                            SocketClient peer = ClientsByEndPoint.Values.Where(x => x.Id == playerId).First();
                                            // Find the other peer when sent the Ack.
                                            SocketClient otherPeer = ClientsByEndPoint[_peerEndPoint];
                                            bool isOtherPeer = !peer.EndPoint.Equals(otherPeer.EndPoint);
                                            if (isOtherPeer)
                                            {
                                                LogHelper.Error("Not Original Client");
                                                if (!ChannelsData[chKey].PacketsToReTransmit.Remove((seqAck, otherPeer.Id), out _))
                                                    LogHelper.Error("Error to Removal");
                                            }
                                            else
                                            {
                                                ChannelsData[chKey].PacketsToReTransmit.Remove((seqAck, playerId), out _);
                                                LogHelper.Error("Original Client");
                                            }
                                        }
                                    }
                                    else
                                    {
                                        // Send the acknowledgement to the remote host to confirm that we received the packet.
                                        // If the Ack is dropped, the remote host will resend the packet.
                                        Send(NeutronStream.Empty, channel, TargetMode.Single, OperationMode.Acknowledgement, playerId, _peerEndPoint, seqAck);
                                        // LogHelper.Error($"The ack with sequence number {seqAck} was sent to the remote host.");
                                        // Read the left data in the packet.
                                        // All the data sent by the remote host is stored in the buffer.
                                        byte[] data = reader.ReadNext();
                                        // Let's process the data and send it to the remote host again.
                                        if (channel == ChannelMode.Reliable)
                                        {
                                            // Let's to check if the packet is a duplicate.
                                            // If the packet is a duplicate, let's ignore it.
                                            // It's necessary to check if the packet is a duplicate, because the Ack can be lost, and the packet will be re-transmitted.
                                            if (!ChannelsData[chKey].ReceivedSequences.TryAdd(seqAck, seqAck))
                                                continue;
                                            // Let's send the data to the remote host.
                                            // Don't send the data to the remote host if the packet is a duplicate.
                                            // Don't send the data is opMode is Acknowledgement or Data.
                                            using (NeutronStream stream = Neutron.PooledNetworkStreams.Pull())
                                            {
                                                var sReader = stream.Reader;
                                                sReader.SetBuffer(data);
                                                OnMessageCompleted?.Invoke(stream, playerId, _peerEndPoint, channel, targetMode, opMode, this);
                                            }
                                        }
                                        else if (channel == ChannelMode.ReliableSequenced)
                                        {
                                            // Let's to check if the packet is a duplicate.
                                            // If the packet is a duplicate, let's ignore it.
                                            // It's necessary to check if the packet is a duplicate, because the Ack can be lost, and the packet will be re-transmitted.
                                            // As the data is sequenced, the verification is easy.
                                            // Ex: If the last processed packet is 100, this means that all data between 0 and 100 was received and processed, sequencing assures us of this.
                                            // Then we can safely ignore and remove packets from 0 to 100.
                                            if (seqAck <= ChannelsData[chKey].LastProcessedSequentialAck)
                                                continue;
                                            // Let's sequence the data and process when the sequence is correct and then remove it.
                                            if (ChannelsData[chKey].SequentialData.TryAdd(seqAck, data))
                                            {
                                                if (ChannelsData[chKey].IsSequential())
                                                {
                                                    var KvPSenquentialData = ChannelsData[chKey].SequentialData.ToList();
                                                    for (int i = 0; i < KvPSenquentialData.Count; i++)
                                                    {
                                                        var KvP = KvPSenquentialData[i];
                                                        if (KvP.Key > ChannelsData[chKey].LastProcessedSequentialAck)
                                                        {
                                                            // Let's process the data and send it to the remote host again.
                                                            using (NeutronStream stream = Neutron.PooledNetworkStreams.Pull())
                                                            {
                                                                var sReader = stream.Reader;
                                                                sReader.SetBuffer(KvP.Value);
                                                                OnMessageCompleted?.Invoke(stream, playerId, _peerEndPoint, channel, targetMode, opMode, this);
                                                            }
                                                            // Increment the last processed sequence.
                                                            // Indicates the last processed packet, used to discard the packets that were already processed.
                                                            // Any value lesser than this value means that the packet was already processed.
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
                                else if (channel == ChannelMode.Unreliable)
                                {
                                    // Read the left data in the packet.
                                    // All the data sent by the remote host is stored in the buffer.
                                    byte[] data = reader.ReadNext();
                                    // Let's process the data and send it to the remote host again.
                                    using (NeutronStream stream = Neutron.PooledNetworkStreams.Pull())
                                    {
                                        var sReader = stream.Reader;
                                        sReader.SetBuffer(data);
                                        OnMessageCompleted?.Invoke(stream, playerId, _peerEndPoint, channel, targetMode, opMode, this);
                                    }
                                }
                            }
                        }
                        else
                            LogHelper.Error("\r\nReceiveFrom() failed with error code: " + bytesTransferred);
                    }
                }
                catch (SocketException ex)
                {
                    if (ex.ErrorCode == 10004)
                        return;

                    LogHelper.Stacktrace(ex);
                }
                catch (ThreadAbortException) { }
                catch (Exception ex)
                {
                    LogHelper.Stacktrace(ex);
                }
            })
            {
                Name = "Neutron_RecvThread",
                Priority = ThreadPriority.Highest,
                IsBackground = true,
            }.Start();
        }

        private bool GetAvailableId(out ushort id)
        {
            if (_ids.Count > 0)
            {
                if (!_ids.TryPull(out id))
                    id = 0;
            }
            else
                id = 0;
            return id > NeutronConstants.GENERATE_PLAYER_ID;
        }

        private void ReturnId(ushort id) => _ids.Push(id, true);

        public Socket GetSocket()
        {
            return _socket;
        }

        public void Close()
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