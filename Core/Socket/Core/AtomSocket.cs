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
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using ThreadPriority = System.Threading.ThreadPriority;

namespace Atom.Core
{
    public sealed class AtomSocket
    {
        internal class AtomClient
        {
            internal AtomClient(int id, long address, int port)
            {
                Id = id;
                EndPoint = new AtomEndPoint(address, port);
            }

            public int Id { get; }
            public EndPoint EndPoint { get; }
        }

        private static readonly Channel[] _channelModes =
        {
             Channel.Unreliable,
             Channel.Reliable,
             Channel.ReliableAndOrderly
         };

        /// <summary> Id of connection, used to identify the connection.</summary>
        private int _id = 0;
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
        private readonly ConcurrentDictionary<EndPoint, AtomClient> _clientsByEndPoint = new();
        private readonly ConcurrentDictionary<int, AtomClient> _clientsById = new();
        /// <summary>Fired when a message is full!</summary>
        public event Action<AtomStream, AtomStream, int, EndPoint, Channel, Target, Operation> OnMessageCompleted;
        /// <summary>
        /// Store the information of the channels.
        /// Ex: SentSequence, RecvSequence, Acknowledge....etc
        /// UDP sequence(seq) and acknowledgment(ack) numbers are used to detect lost packets and to detect packet reordering.
        /// The sequence number is incremented every time a packet is sent, and the acknowledgment number is incremented every time a packet is received.
        /// The acknowledgment number is used to confirm that the packet has been received, if not, the packet is resent.
        /// The sequence number is used to reorder packets, if the packet is out of order, the packet is reordered.
        /// </summary>
        private readonly ConcurrentDictionary<(int, byte), AtomChannel> _channels = new();
        /// <summary>List of exlusive id's, used to prevent the same id to be used twice.</summary>
        private readonly AtomSafelyQueue<int> _ids = new(true);
        /// <summary> Returns whether the "Instance" is the Server or the Client. </summary>
        public bool IsServer { get; private set; }

#pragma warning disable IDE1006
        private void __Constructor__(EndPoint endPoint)
#pragma warning restore IDE1006
        {
            _socket = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
            {
                // The ReceiveBufferSize property gets or sets the number of bytes that you are expecting to store in the receive buffer for each read operation. 
                ReceiveBufferSize = AtomGlobal.Settings.MaxRecBuffer,
                // The SendBufferSize property gets or sets the number of bytes that you are expecting to store in the send buffer for each send operation.
                SendBufferSize = AtomGlobal.Settings.MaxSendBuffer,
            };
            _socket.Bind(endPoint);
            _cancelTokenSource = new();
            // Add the availables id's to the list.
            // This list is used to prevent the same id to be used twice.
            for (int i = 1; i < AtomGlobal.Settings.MaxPlayers; i++)
                _ids.Push(i, false);
            _ids.Sort();
        }

        public void Initialize(string address, int port, bool isServer)
        {
            IsServer = isServer;
            // Initialize the constructor!
            __Constructor__(new IPEndPoint(IPAddress.Parse(address), port));
            // Start the receive thread.
            // The Unity API doesn't allow to be called from a thread other than the main thread.
            // The Unity API wil be dispatched to the main thread.
            // Why don't receive the data in the main thread?
            // Because the ReceiveFrom() method is blocking, FPS will be affected.
            // The Unity will be frozen until the data is received, but's not a good idead, right?
            Receive();
        }

        public IEnumerator Connect(string address, int port)
        {
            _destEndPoint = new AtomEndPoint(IPAddress.Parse(address), port);
            while (true)
            {
                using (AtomStream message = AtomStream.Get())
                {
                    message.Write((byte)Message.ConnectAndPing);
                    message.Write(AtomTime.LocalTime);
                    // The first packet is used to establish the connection.
                    // We are using unrealible channel, because we don't have and exclusive Id for the connection.
                    // We need an id to identify the connection, and the id is the "symbolic link" for the EndPoint...
                    // As we are using an unrealible channel, we need to send connection packets until we get a response.
                    SendToServer(message, Channel.Unreliable, Target.Single);
                }
                yield return new WaitForSeconds(0.2f);
            }
        }

        public Message OnServerMessageCompleted(AtomStream reader, AtomStream writer, int playerId, EndPoint endPoint, Channel channelMode, Target targetMode, Operation opMode)
        {
            reader.Read(out byte value);
            Message message = (Message)value;
            switch (message)
            {
                case Message.ConnectAndPing:
                    {
                        Debug.Log("Client dd!");
                        if (playerId == 0)
                        {
                            if (_clientsByEndPoint.TryRemove(endPoint, out AtomClient socketClient) && _clientsById.TryRemove(socketClient.Id, out _))
                            {
                                RemoveChannel(socketClient.Id);
                                ReturnId(socketClient.Id);
                            }

                            if (GetAvailableId(out int id))
                            {
                                if (endPoint is AtomEndPoint _endPoint)
                                {
                                    long address = _endPoint.GetIPAddress();
                                    int port = _endPoint.GetPort();
                                    AtomClient client = new(id, address, port);
                                    if (_clientsByEndPoint.TryAdd(client.EndPoint, client) && _clientsById.TryAdd(id, client))
                                    {
                                        AddChannel(id);
                                        writer.Write((byte)Message.ConnectAndPing);
                                        writer.Write(id);
                                        SendToClient(writer, channelMode, targetMode, opMode, id);
                                    }
                                    else
                                        Debug.LogError("Client not added!");
                                }
                            }
                            else
                                Debug.LogError("No available id's!");
                        }
                        else
                        {
                            Send(playerId);
                            // The client is already connected, so we need to send the ping.
                            reader.Read(out double timeOfClient);
                            writer.Write((byte)Message.ConnectAndPing);
                            writer.Write(timeOfClient);
                            writer.Write(AtomTime.LocalTime);
                            SendToClient(writer, channelMode, targetMode, opMode, playerId);
                        }
                    }
                    break;
                default:
                    Debug.Log("Test");
                    return message;
            }

            return 0;
        }

        public Message OnClientMessageCompleted(AtomStream reader, AtomStream writer, int playerId, EndPoint endPoint, Channel channelMode, Target targetMode, Operation opMode)
        {
            reader.Read(out byte value);
            Message message = (Message)value;
            switch (message)
            {
                case Message.ConnectAndPing:
                    if (_id == 0)
                    {
                        _id.Read(reader);
                        for (int i = 0; i < AtomGlobal.Settings.MaxPlayers; i++)
                            AddChannel(i);
                    }
                    else
                    {
                        Send(playerId);
                        // The client is already connected, calculate the ping.
                        reader.Read(out double timeOfClient);
                        reader.Read(out double timeOfServer);
                        AtomTime.GetNetworkTime(timeOfClient, timeOfServer);
                    }
                    break;
                default:
                    return message; // If not an private packet, return the packet type to process it.
            }
            // If is a private packet, return null to ignore it.
            return default;
        }

        private void AddChannel(int playerId)
        {
            for (int i = 0; i < _channelModes.Length; i++)
            {
                Channel channelMode = _channelModes[i];
                if (!_channels.TryAdd((playerId, (byte)channelMode), new AtomChannel()))
                    Debug.LogError($"Channel {channelMode} already exists!");
            }
        }

        private void RemoveChannel(int playerId)
        {
            for (int i = 0; i < _channelModes.Length; i++)
            {
                Channel channelMode = _channelModes[i];
                if (!_channels.TryRemove((playerId, (byte)channelMode), out _))
                    Debug.LogError($"Channel {channelMode} not found!");
            }
        }

        public void SendToClient(AtomStream message, Channel channel, Target targetMode, Operation opMode, int playerId)
        {
            if (opMode == Operation.Sequence)
                SendToClient(message, channel, targetMode, playerId, null);
            else
                Debug.LogError("Sequence operation not supported!");
        }

        public void SendToServer(AtomStream message, Channel channel, Target targetMode)
        {
#if ATOM_DEBUG
            if (((channel == Channel.ReliableAndOrderly || channel == Channel.Reliable) && _id == 0) || _destEndPoint == null)
                throw new Exception("[Atom] -> You must connect to the server before sending data.");
#endif
            Send(message, channel, targetMode, Operation.Sequence, _id, _destEndPoint, 0);
        }

        private void SendToClient(AtomStream message, Channel channel, Target targetMode, int playerId, EndPoint endPoint)
        {
            Send(message, channel, targetMode, Operation.Data, playerId, endPoint, 0);
        }

        private void Send(AtomStream messageStream, Channel channelMode, Target targetMode, Operation opMode, int playerId, EndPoint endPoint, int seqAck = 0)
        {
            var data = messageStream.GetBufferAsReadOnlySpan();
            int defSize = (channelMode == Channel.ReliableAndOrderly || channelMode == Channel.Reliable) ? AtomCore.RELIABLE_SIZE : AtomCore.UNRELIABLE_SIZE;
#if ATOM_DEBUG
            if (messageStream.FixedSize)
            {
                if ((data.Length + defSize) != messageStream.Size)
                    throw new Exception("[Atom] -> The size of the packet is not correct. You setted " + messageStream.Size + " but you need " + (data.Length + defSize) + ".");
            }
#endif
            messageStream.Reset(pos: defSize);
            messageStream.Write(data);
            messageStream.Position = 0;
#if ATOM_DEBUG
            if (((byte)channelMode) > AtomCore.CHANNEL_MASK || ((byte)targetMode) > AtomCore.TARGET_MASK || ((byte)opMode) > AtomCore.OPERATION_MASK)
                throw new Exception("[Atom] Send -> The channelMode, targetMode or opMode is not correct.");
#endif
            byte header = (byte)((byte)channelMode | (byte)targetMode << 2 | (byte)opMode << 5);
            messageStream.Write(header);
#if ATOM_BYTE_PLAYER_ID
            messageStream.Write((byte)playerId);
#elif ATOM_USHORT_PLAYER_ID
            messageStream.Write((ushort)playerId);
#elif ATOM_INT_PLAYER_ID
            messageStream.Write((int)playerId);
#endif
            switch (channelMode)
            {
                case Channel.Reliable:
                case Channel.ReliableAndOrderly:
                    {
                        AtomChannel channelData = _channels[(playerId, (byte)channelMode)];
                        if (seqAck == 0)
                            seqAck = Interlocked.Increment(ref channelData.SentAck);
                        messageStream.Write(seqAck);
                        break;
                    }
            }

            messageStream.Position = messageStream.CountBytes;
            byte[] _data = messageStream.GetBufferAsCopy();
            Send(_data, endPoint, targetMode, channelMode, opMode, playerId, seqAck, false);
        }

        private void Send(int playerId)
        {
            for (int i = 0; i < _channelModes.Length; i++)
            {
                Channel channel = _channelModes[i];
                var messages = _channels[(playerId, (byte)channel)].MessagesToRelay.Values.ToList();
                for (int y = 0; y < messages.Count; y++)
                {
                    var relayMessage = messages[y];
                    Send(isRelay: true, data: relayMessage.Data, endPoint: relayMessage.EndPoint, target: Target.Single, playerId: relayMessage.Id);
                }
            }
        }

        private void Send(byte[] data = default, EndPoint endPoint = default, Target target = default, Channel channel = default, Operation opMode = default, int playerId = default, int seqAck = default, bool isRelay = default)
        {
            void CreateRelayMessage(int _playerId, EndPoint endPoint)
            {
                if (channel == Channel.Reliable || channel == Channel.ReliableAndOrderly)
                {
                    // Don't create the transmission packet in Ack packets. 
                    // because we are retransmitting the packet to the owner of the packet, in this case,
                    // who takes care of the retransmission is the owner of the packet, 
                    // so we can't re-transmit the packet because it already does that, otherwise it's in a retransmission loop.
                    // Only clients who do not own(owner) the packet can re-transmit the packet.
                    if (opMode == Operation.Acknowledgement)
                        return;

                    byte[] _data = new byte[data.Length];
                    Buffer.BlockCopy(data, 0, _data, 0, data.Length);
                    if (!_channels[(playerId, (byte)channel)].MessagesToRelay.TryAdd((seqAck, _playerId), new(_playerId, _data, endPoint)))
                        Debug.LogError("[Atom] -> Relay message already exists!");
                }
            }

            if (IsServer)
            {
                EndPoint _endPoint = _clientsById[playerId].EndPoint;
                switch (target)
                {
                    case Target.All:
                        if (!isRelay) CreateRelayMessage(playerId, _endPoint);
                        _socket.SendTo(data, _endPoint);
                        foreach (var KvP in _clientsByEndPoint.ToList())
                        {
                            if (!KvP.Key.Equals(_endPoint))
                            {
                                AtomClient client = KvP.Value;
                                if (!isRelay) CreateRelayMessage(client.Id, client.EndPoint);
                                _socket.SendTo(data, client.EndPoint);
                            }
                            else
                                continue;
                        }
                        break;
                    case Target.Others:
                        break;
                    case Target.Single:
                        {
                            if (!isRelay) CreateRelayMessage(playerId, _endPoint);
                            _socket.SendTo(data, _endPoint);
                        }
                        break;
                }
            }
            else
            {
                if (!isRelay) CreateRelayMessage(playerId, endPoint);
                _socket.SendTo(data, endPoint);
            }
        }

        private void Receive()
        {
            new Thread(() =>
            {
                try
                {
#if ATOM_BANDWIDTH_COUNTER
                    // Bandwidth Control for incoming data.
                    AtomBandwidth bandwidthCounter = new();
#endif
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
#if ATOM_BANDWIDTH_COUNTER
                        bandwidthCounter.Start();
#endif
                        int bytesTransferred = _socket.ReceiveFrom(buffer, ref _peerEndPoint);
                        if (bytesTransferred >= 0)
                        {
#if ATOM_BANDWIDTH_COUNTER
                            bandwidthCounter.Stop();
                            bandwidthCounter.Add(bytesTransferred);
                            bandwidthCounter.Get(out int bytesRate, out int messageRate);
#endif
#if UNITY_SERVER
                            if (bytesRate > 0 && messageRate > 0)
                                Console.WriteLine($"Avg: Rec {bytesTransferred} bytes, {bytesRate} bytes/s, {messageRate} messages/s");
#else
                            if (bytesRate > 0 && messageRate > 0)
                                Debug.LogFormat(LogType.Error, LogOption.NoStacktrace, null, "Avg: Rec {0} bytes | {1} bytes/s | {2} messages/s", bytesTransferred, bytesRate, messageRate);
#endif
                            int playerId = 0;
                            using (AtomStream atomStream = AtomStream.Get())
                            {
                                atomStream.SetBuffer(_buffer[..bytesTransferred]);
                                atomStream.Read(out byte header);
#if ATOM_BYTE_PLAYER_ID
                                atomStream.Read(out byte _playerId);
                                playerId = _playerId;
#elif ATOM_USHORT_PLAYER_ID
                                atomStream.Read(out ushort _playerId);
                                playerId = _playerId;
#elif ATOM_INT_PLAYER_ID
                                atomStream.Read(out int _playerId);
                                playerId = _playerId;
#endif
                                // Decode the header.
                                Channel channelMode = (Channel)(byte)(header & AtomCore.CHANNEL_MASK);
                                Target targetMode = (Target)(byte)((header >> 2) & AtomCore.TARGET_MASK);
                                Operation opMode = (Operation)(byte)((header >> 5) & AtomCore.OPERATION_MASK);
#if ATOM_DEBUG
                                if (((byte)channelMode) > AtomCore.CHANNEL_MASK || ((byte)targetMode) > AtomCore.TARGET_MASK || ((byte)opMode) > AtomCore.OPERATION_MASK)
                                    throw new Exception("[Atom] Send -> The channelMode, targetMode or opMode is not correct.");
#endif
                                switch (channelMode)
                                {
                                    case Channel.Reliable:
                                    case Channel.ReliableAndOrderly:
                                        {
                                            AtomChannel channel = _channels[(playerId, (byte)channelMode)];
                                            atomStream.Read(out int seqAck);
                                            if (opMode == Operation.Acknowledgement)
                                            {
                                                if (!IsServer)
                                                    channel.MessagesToRelay.TryRemove((seqAck, playerId), out _);
                                                else
                                                {
                                                    AtomClient peer = _clientsById[playerId];
                                                    AtomClient otherPeer = _clientsByEndPoint[_peerEndPoint];
                                                    bool isOtherPeer = !peer.EndPoint.Equals(_peerEndPoint);
                                                    int peerId = isOtherPeer ? otherPeer.Id : playerId;
                                                    channel.MessagesToRelay.TryRemove((seqAck, peerId), out _);
                                                }
                                            }
                                            else
                                            {
                                                ReadOnlySpan<byte> data = atomStream.ReadNext();
                                                using (AtomStream ackStream = AtomStream.Get())
                                                    Send(ackStream, channelMode, Target.Single, Operation.Acknowledgement, playerId, _peerEndPoint, seqAck);

                                                switch (channelMode)
                                                {
                                                    case Channel.Reliable:
                                                        {
                                                            if (!channel.Acknowledgements.Add(seqAck))
                                                                continue;

                                                            //if()

                                                            using (AtomStream reader = AtomStream.Get())
                                                            {
                                                                using (AtomStream writer = AtomStream.Get())
                                                                {
                                                                    reader.SetBuffer(data);
                                                                    OnMessageCompleted?.Invoke(reader, writer, playerId, _peerEndPoint, channelMode, targetMode, opMode);
                                                                }
                                                            }

                                                            break;
                                                        }

                                                    case Channel.ReliableAndOrderly:
                                                        {
                                                            if (seqAck <= channel.LastProcessedSequentialAck)
                                                                continue;

                                                            if (channel.SequentialData.ContainsKey(seqAck))
                                                            {
                                                                channel.SequentialData.Add(seqAck, data.ToArray());
                                                                if (channel.IsSequential())
                                                                {
                                                                    var KvPSenquentialData = channel.SequentialData.ToList();
                                                                    for (int i = 0; i < KvPSenquentialData.Count; i++)
                                                                    {
                                                                        var KvP = KvPSenquentialData[i];
                                                                        if (KvP.Key > channel.LastProcessedSequentialAck)
                                                                        {
                                                                            using (AtomStream reader = AtomStream.Get())
                                                                            {
                                                                                using (AtomStream writer = AtomStream.Get())
                                                                                {
                                                                                    reader.SetBuffer(KvP.Value);
                                                                                    OnMessageCompleted?.Invoke(reader, writer, playerId, _peerEndPoint, channelMode, targetMode, opMode);
                                                                                }
                                                                            }
                                                                            channel.LastProcessedSequentialAck++;
                                                                        }
                                                                        else
                                                                            channel.SequentialData.Remove(KvP.Key);
                                                                    }
                                                                    channel.LastReceivedSequentialAck = KvPSenquentialData.Last().Key;
                                                                }
                                                                else {/*Waiting for more packets, to create the sequence*/}
                                                            }
                                                            else {/*Discard duplicate packet....*/}

                                                            break;
                                                        }
                                                }
                                            }

                                            break;
                                        }

                                    case Channel.Unreliable:
                                        {
                                            ReadOnlySpan<byte> data = atomStream.ReadNext();
                                            using (AtomStream reader = AtomStream.Get())
                                            {
                                                using (AtomStream writer = AtomStream.Get())
                                                {
                                                    reader.SetBuffer(data);
                                                    OnMessageCompleted?.Invoke(reader, writer, playerId, _peerEndPoint, channelMode, targetMode, opMode);
                                                }
                                            }

                                            break;
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

        private bool GetAvailableId(out int id)
        {
            id = 0;
            if (_ids.Count > 0)
                id = _ids.Pull();
            return id != 0;
        }

        private void ReturnId(int id) => _ids.Push(id, true);
        public Socket GetSocket() => _socket;
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