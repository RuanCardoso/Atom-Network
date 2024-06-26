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
using Atom.Core.Interface;
using Atom.Core.Wrappers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using static Atom.Core.AtomCore;
using static Atom.Core.AtomGlobal;
using ThreadPriority = System.Threading.ThreadPriority;

namespace Atom.Core
{
    public sealed class AtomSocket : ISocket
    {
        internal class AtomClient
        {
            public AtomClient(int id)
            {
                Id = id;
            }

            public int Id { get; }
            public bool IsConnected { get; internal set; }
            public EndPoint EndPoint { get; internal set; }
        }

        private static readonly Channel[] _channelModes =
        {
             Channel.Unreliable,
             Channel.Reliable,
             Channel.ReliableAndOrderly
         };

        private MonoBehaviour _this;
        /// <summary> Id of connection, used to identify the connection.</summary>
        private int _id = 0;
        /// <summary>\
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
        private readonly Dictionary<EndPoint, AtomClient> _clientsByEndPoint = new(); // Thread safe, no need to lock, does not have simultaneous access.
        private readonly Dictionary<int, AtomClient> _clientsById = new(); // Thread safe, no need to lock, it's readonly.
        private readonly List<AtomClient> _clientsByIdToList = new(); // Thread safe, no need to lock, it's readonly.
        /// <summary> Store the information of the channels. </summary>
        private readonly Dictionary<(int, byte), AtomChannel> _channels = new(); // Thread safe, no need to lock, it's readonly.
        /// <summary>List of exlusive id's, used to prevent the same id to be used twice.</summary>
        private readonly AtomSafelyQueue<int> _ids = new(true);
        /// <summary> Returns whether the "Instance" is the Server or the Client. </summary>
        internal bool IsServer { get; private set; }

        private readonly ISocket ISocket;
        public AtomSocket(ISocket iSocket) => ISocket = iSocket;
#pragma warning disable IDE1006
        private void __Constructor__(EndPoint endPoint)
#pragma warning restore IDE1006
        {
            _pingWaiter = new(Conf.PingFrequency);
            _socket = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
            {
                ReceiveBufferSize = Conf.MaxRecBuffer,
                ReceiveTimeout = Conf.ReceiveTimeout,
                SendBufferSize = Conf.MaxSendBuffer,
                SendTimeout = Conf.SendTimeout,
            };
            _cancelTokenSource = new();
            _socket.Bind(endPoint);
            // Add the availables id's to the list.
            // This list is used to prevent the same id to be used twice.
            for (int idOfPlayer = 1; idOfPlayer <= Conf.MaxPlayers; idOfPlayer++)
            {
                _ids.Push(idOfPlayer, false);
                // Pre-alloc memory!
                AddChannel(idOfPlayer); // Thread safe after the initialization, because it's readonly.
                // Pre-alloc memory!
                _clientsById.Add(idOfPlayer, new(idOfPlayer)); // Thread safe after the initialization, because it's readonly.
            }
            _clientsByIdToList.AddRange(_clientsById.Values);
            // Sort the id's to prevent the same id to be used twice.
            _ids.Sort();
        }

        public void Initialize(string address, int port, bool isServer, MonoBehaviour _this)
        {
            this._this = _this;
            IsServer = isServer;
            // Initialize the constructor!
            __Constructor__(new IPEndPoint(IPAddress.Parse(address), port));
            this._this.StartCoroutine(Relay()); // Start relaying the messages to the clients.
            // Start the receive thread.
            // The Unity API doesn't allow to be called from a thread other than the main thread.
            // The Unity API wil be dispatched to the main thread.
            // Why don't receive the data in the main thread?
            // Because the ReceiveFrom() method is blocking, FPS will be affected.
            // The Unity will be frozen until the data is received, but's not a good idead, right?
            Receive();
        }

        public void Connect(string address, int port)
        {
            IsServer = false;
            /**************************************************/
            _this.StartCoroutine(ConnectAndPing(address, port));
        }

        private WaitForSeconds _pingWaiter;
        private IEnumerator ConnectAndPing(string address, int port)
        {
            _destEndPoint = new AtomEndPoint(IPAddress.Parse(address), port);
            while (true)
            {
                if (_id != 0) AtomTime.AddSent();
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
                yield return _pingWaiter;
            }
        }

        private IEnumerator Relay()
        {
            while (true)
            {
                if (!IsServer && _id == 0)
                {
                    yield return null;
                    continue;
                }
#if UNITY_SERVER || UNITY_EDITOR
                for (int pId = 1; pId <= Conf.MaxPlayers; pId++)
#else
                int pId = _id;
#endif
                {
#if UNITY_SERVER || UNITY_EDITOR
                    if (!_clientsById[pId].IsConnected)
                        continue;
#endif
                    for (int channelIndex = 1; channelIndex < _channelModes.Length; channelIndex++)
                    {
                        Channel channel = _channelModes[channelIndex];
                        AtomChannel atomChannel = _channels[(pId, (byte)channel)];
                        foreach (var (msgKey, message) in atomChannel.MessagesToRelay)
                        {
                            if (DateTime.UtcNow.Subtract(message.LastTime).TotalSeconds >= 0.2d)
                            {
                                if (message.PlayersToRelay.Count > 0)
                                {
                                    foreach (var (idOfPlayer, _) in message.PlayersToRelay)
                                    {
                                        int bytesWritten = message.BytesWritten;
                                        byte[] buffer = message.GetBuffer();
                                        Send(buffer, bytesWritten, Target.Single, channel, !IsServer ? Operation.Sequence : Operation.Data, idOfPlayer, 0, true);
                                        message.LastTime = DateTime.UtcNow;
                                        AtomLogger.Print($"Relaying message to {idOfPlayer} {IsServer}");
                                    }
                                }
                                else
                                {
                                    if (_channels[(pId, (byte)channel)].MessagesToRelay.TryRemove(msgKey, out _))
                                    {
                                        message.Reset();
                                        message.PlayersToRelay.Clear();
                                        StreamsToWaitAck.Push(message);
                                    }
                                }
                            }
                            else
                                continue;
                        }
                    }
                }
                yield return null;
            }
        }

        public void OnMessageCompleted(AtomStream reader, AtomStream writer, int playerId, EndPoint endPoint, Channel channelMode, Target targetMode, Operation opMode, bool isServer)
        {
            Message message = (Message)reader.ReadByte();
            switch (message)
            {
                case Message.ConnectAndPing:
                    {
                        if (isServer)
                        {
                            if (playerId == 0)
                            {
                                if (_clientsByEndPoint.Remove(endPoint, out AtomClient disClient))
                                {
                                    disClient.IsConnected = false;
                                    disClient.EndPoint = default;
                                    ReturnId(disClient.Id);
                                }

                                if (GetAvailableId(out int id))
                                {
                                    if (endPoint is AtomEndPoint _endPoint)
                                    {
                                        AtomClient socketClient = _clientsById[id];
                                        socketClient.IsConnected = true;
                                        socketClient.EndPoint = new AtomEndPoint(_endPoint.GetIPAddress(), _endPoint.GetPort());
                                        _clientsByEndPoint.Add(socketClient.EndPoint, socketClient);
                                        /**********************************************************/
                                        double timeOfClient = reader.ReadDouble();
                                        writer.Write((byte)Message.ConnectAndPing);
                                        writer.Write(id);
                                        writer.Write(timeOfClient);
                                        writer.Write(AtomTime.LocalTime);
                                        SendToClient(writer, channelMode, targetMode, opMode, id);
                                    }
                                    else
                                        throw new Exception("The endPoint is not an AtomEndPoint.");
                                }
                                else
                                    throw new Exception("No available id.");
                            }
                            else
                            {
                                double timeOfClient = reader.ReadDouble();
                                writer.Write((byte)Message.ConnectAndPing);
                                writer.Write(timeOfClient);
                                writer.Write(AtomTime.LocalTime);
                                SendToClient(writer, channelMode, targetMode, opMode, playerId);
                            }
                        }
                        else if (!isServer)
                        {
                            if (_id == 0)
                            {
                                if (_destEndPoint is AtomEndPoint _endPoint)
                                {
                                    _id.Read(reader);
                                    AtomClient socketClient = _clientsById[_id];
                                    socketClient.IsConnected = true;
                                    socketClient.EndPoint = new AtomEndPoint(_endPoint.GetIPAddress(), _endPoint.GetPort());
                                    /***************************************************************************************/
                                    double timeOfClient = reader.ReadDouble();
                                    double timeOfServer = reader.ReadDouble();
                                    AtomTime.SetTime(timeOfClient, timeOfServer);
                                }
                            }
                            else
                            {
                                double timeOfClient = reader.ReadDouble();
                                double timeOfServer = reader.ReadDouble();
                                AtomTime.SetTime(timeOfClient, timeOfServer);
                                AtomTime.AddReceived();
                            }
                        }
                    }
                    break;
                default:
                    reader.Position = 0;
                    ISocket.OnMessageCompleted(reader, writer, playerId, endPoint, channelMode, targetMode, opMode, IsServer);
                    break;
            }
        }

        private void AddChannel(int playerId)
        {
            for (int i = 0; i < _channelModes.Length; i++)
            {
                Channel channelMode = _channelModes[i];
                if (channelMode == Channel.Reliable || channelMode == Channel.ReliableAndOrderly)
                {
                    if (!_channels.TryAdd((playerId, (byte)channelMode), new()))
                        AtomLogger.PrintError($"Channel {channelMode} already exists!");
                }
            }
        }

        public void SendToClient(AtomStream message, Channel channel, Target targetMode, Operation opMode = Operation.Sequence, int playerId = 0)
        {
#if ATOM_DEBUG
            if (playerId == 0)
                throw new Exception("Can't send to client 0!");
            if (opMode != Operation.Sequence)
                throw new Exception("Can't send to client with non-sequential operation!");
#endif
            Send(message, channel, targetMode, Operation.Data, playerId, 0);
        }

        public void SendToServer(AtomStream message, Channel channel, Target target)
        {
#if ATOM_DEBUG
            if (((channel == Channel.ReliableAndOrderly || channel == Channel.Reliable) && _id == 0) || _destEndPoint == null)
                throw new Exception("[Atom] -> You must connect to the server before sending data.");
#endif
            Send(message, channel, target, Operation.Sequence, _id, 0);
        }

        private void Send(AtomStream messageStream, Channel channelMode, Target targetMode, Operation opMode, int playerId, int seqAck = 0)
        {
            if (IsServer && targetMode == Target.Server) return;
            var data = messageStream.GetBuffer();
            int defSize = (channelMode == Channel.ReliableAndOrderly || channelMode == Channel.Reliable) ? RELIABLE_SIZE : UNRELIABLE_SIZE;
#if ATOM_DEBUG
            if (messageStream.FixedSize && (data.Length + defSize) != messageStream.Length)
                throw new Exception("[Atom] -> The size of the packet is not correct. You setted " + messageStream.Length + " but you need " + (data.Length + defSize) + ".");
#endif
            int bytesWritten = messageStream.BytesWritten;
            messageStream.Reset(pos: defSize);
            messageStream.Write(data, 0, bytesWritten);
            messageStream.Position = 0;
#if ATOM_DEBUG
            if (((byte)channelMode) > CHANNEL_MASK || ((byte)targetMode) > TARGET_MASK || ((byte)opMode) > OPERATION_MASK)
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
#else
            messageStream.Write((byte)playerId);
#endif
            AtomChannel atomChannel = default;
            if (channelMode == Channel.Reliable || channelMode == Channel.ReliableAndOrderly)
            {
                atomChannel = _channels[(playerId, (byte)channelMode)];
                if (seqAck == 0)
                    seqAck = Interlocked.Increment(ref atomChannel.nextSeqAck);
                messageStream.Write(seqAck);
            }
            /*************************************************************************************/
            messageStream.Position = messageStream.BytesWritten;
            byte[] _data = messageStream.GetBuffer();
            bytesWritten = messageStream.BytesWritten;
            Send(_data, bytesWritten, targetMode, channelMode, opMode, playerId, seqAck, false);
        }

        private object _lock = new();
        private void Send(byte[] data = default, int length = default, Target target = default, Channel channel = default, Operation operation = default, int playerId = default, int seqAck = default, bool isRelay = default)
        {
            int sendTo = default;
            bool isReliable = channel == Channel.Reliable || channel == Channel.ReliableAndOrderly;
            bool isValid = isReliable && !isRelay && operation != Operation.Acknowledgement;
            EndPoint endPoint = IsServer ? _clientsById[playerId].EndPoint : _destEndPoint;
            /*************************************************************************************************************************************/
            AtomStream waitAck = default;
            // Used to relay messages to peers!
            // If the message is unreliable, we just send it.
            // If the operation is acknowledgement, disable the waiting for the acknowledgement, to avoid a loop of acknowledgements.
            if (isValid)
            {
                waitAck = StreamsToWaitAck.Pull();
                waitAck.Write(data, 0, length);
                waitAck.LastTime = DateTime.UtcNow;
                AtomChannel atomChannel = _channels[(playerId, (byte)channel)];
                if (!atomChannel.MessagesToRelay.TryAdd(seqAck, waitAck))
                    AtomLogger.PrintError("[Atom] -> The message with the sequence " + seqAck + " already exists!");
            }
            /*************************************************************************************************************************************/
            lock (_lock)
            {
                if (IsServer)
                {
                    switch (target)
                    {
                        case Target.All:
                            {
                                for (int i = 0; i < _clientsByIdToList.Count; i++)
                                {
                                    AtomClient client = _clientsByIdToList[i];
                                    if (!client.IsConnected)
                                        return;
                                    endPoint = _clientsById[client.Id].EndPoint;
                                    if (isValid) waitAck.PlayersToRelay.TryAdd(client.Id, client.Id);
                                    sendTo = _socket.SendTo(data, length, SocketFlags.None, endPoint);
                                }
                            }
                            break;
                        case Target.Others:
                            break;
                        case Target.Single:
                            {
                                if (isValid) waitAck.PlayersToRelay.TryAdd(playerId, playerId);
                                sendTo = _socket.SendTo(data, length, SocketFlags.None, endPoint);
                            }
                            break;
                    }
                }
                else
                {
                    if (isValid) waitAck.PlayersToRelay.TryAdd(playerId, playerId);
                    sendTo = _socket.SendTo(data, length, SocketFlags.None, endPoint);
                }
            }
#if ATOM_DEBUG
            if (sendTo != length)
                AtomLogger.PrintError("[Atom] -> Send -> The data was not sent correctly. Sent " + sendTo + " bytes but it should have sent " + length + " bytes.");
#endif
        }

        private void Receive()
        {
            new Thread(() =>
            {
                void internal_iCall(byte[] data, int length, int playerId, EndPoint endPoint, Channel channel, Target target, Operation operation, bool isServer)
                {
                    using AtomStream reader = AtomStream.Get();
                    using AtomStream writer = AtomStream.Get();
                    reader.SetBuffer(data, 0, length);
                    OnMessageCompleted(reader, writer, playerId, endPoint, channel, target, operation, IsServer);
                }

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
                    // The size of the buffer is the maximum size of the data that can be received(MTU Size).
                    byte[] buffer = new byte[1536];
                    while (!_cancelTokenSource.IsCancellationRequested)
                    {
                        int bytesTransferred = _socket.ReceiveFrom(buffer, ref _peerEndPoint);
                        /*********************************************************************/
                        if (Module.Drop() && IsServer)
                            continue;
                        if (Module.IsOn && (IsServer && Module.SimulateOnServer)
                        || Module.IsOn && (!IsServer && Module.SimulateOnClient))
                            Thread.Sleep(Module.DelayPercentage);
                        /*********************************************************************/
                        if (bytesTransferred >= 0)
                        {
#if ATOM_BANDWIDTH_COUNTER
                            bandwidthCounter.Add(bytesTransferred, AtomTime.LocalTime);
                            if (bandwidthCounter.TotalMessages > 0 && !double.IsInfinity(bandwidthCounter.TotalMessages))
                            {
#if UNITY_EDITOR
                                if (IsServer)
                                {
                                    Module.SERVER_REC_BYTES_RATE = $"{bandwidthCounter.BytesTransferred} bytes/s";
                                    Module.SERVER_REC_MSG_RATE = $"{bandwidthCounter.TotalMessages} messages/s";
                                }
                                else
                                {
                                    Module.CLIENT_REC_BYTES_RATE = $"{bandwidthCounter.BytesTransferred} bytes/s";
                                    Module.CLIENT_REC_MSG_RATE = $"{bandwidthCounter.TotalMessages} messages/s";
                                }
#endif
                            }
#endif
                            using AtomStream message = AtomStream.Get();
                            message.SetBuffer(buffer, 0, bytesTransferred);
                            var ENCODED_HEADER = message.ReadByte();
#if ATOM_BYTE_PLAYER_ID
                            int playerId = message.ReadByte();
#elif ATOM_USHORT_PLAYER_ID
                            int playerId = message.ReadUShort();
#elif ATOM_INT_PLAYER_ID
                            int playerId = message.ReadInt();
#else
                            int playerId = message.ReadByte();
#endif
                            Channel channelMode = (Channel)(byte)(ENCODED_HEADER & CHANNEL_MASK);
                            Target targetMode = (Target)(byte)((ENCODED_HEADER >> 2) & TARGET_MASK);
                            Operation opMode = (Operation)(byte)((ENCODED_HEADER >> 5) & OPERATION_MASK);
#if ATOM_DEBUG
                            if (((byte)channelMode) > CHANNEL_MASK || ((byte)targetMode) > TARGET_MASK || ((byte)opMode) > OPERATION_MASK)
                                throw new Exception("[Atom] Send -> The channelMode, targetMode or opMode is not correct.");
#endif
                            switch (channelMode)
                            {
                                case Channel.Reliable:
                                case Channel.ReliableAndOrderly:
                                    {
                                        AtomChannel atomChannel = _channels[(playerId, (byte)channelMode)];
                                        int seqAck = message.ReadInt();
                                        if (opMode == Operation.Acknowledgement)
                                        {
                                            AtomClient peer = IsServer ? _clientsByEndPoint[_peerEndPoint] : _clientsById[playerId];
                                            if (atomChannel.MessagesToRelay.TryGetValue(seqAck, out AtomStream messageToRelay))
                                            {
                                                if (!messageToRelay.PlayersToRelay.TryRemove(peer.Id, out _)) { }
                                            }
                                        }
                                        else
                                        {
                                            byte[] data = message.ReadNext(out int length);
                                            using AtomStream ackStream = AtomStream.Get();
                                            Send(ackStream, channelMode, Target.Single, Operation.Acknowledgement, playerId, seqAck);
                                            switch (channelMode)
                                            {
                                                case Channel.Reliable:
                                                    {
                                                        if (!atomChannel.ReliableAcks.Add(seqAck))
                                                            continue;

                                                        internal_iCall(data, length, playerId, _peerEndPoint, channelMode, targetMode, opMode, IsServer);
                                                        break;
                                                    }

                                                case Channel.ReliableAndOrderly:
                                                    {
                                                        HashSet<int> reliableAcks = atomChannel.ReliableAcks;
                                                        if ((seqAck < atomChannel.ExpectedAck) || !reliableAcks.Add(seqAck))
                                                            continue;

                                                        int min = reliableAcks.Min();
                                                        int max = reliableAcks.Max();
                                                        AtomStream orderlyStream = StreamsToWaitAck.Pull();
                                                        orderlyStream.Write(data, 0, length);
                                                        atomChannel.SequentialData.Add(seqAck, orderlyStream);
                                                        if (min == atomChannel.ExpectedAck)
                                                        {
                                                            int range = max - (min - 1);
                                                            if (reliableAcks.Count == range)
                                                            {
                                                                foreach (var (_, seqStream) in atomChannel.SequentialData)
                                                                {
                                                                    byte[] _data_ = seqStream.GetBuffer();
                                                                    internal_iCall(_data_, seqStream.BytesWritten, playerId, _peerEndPoint, channelMode, targetMode, opMode, IsServer);
                                                                    seqStream.Reset();
                                                                    StreamsToWaitAck.Push(seqStream);
                                                                }

                                                                atomChannel.ExpectedAck = max + 1;
                                                                reliableAcks.Clear();
                                                                atomChannel.SequentialData.Clear();
                                                            }
                                                            else { /* Get missing messages  */}
                                                        }
                                                        else { /* Get missing messages  */}
                                                        break;
                                                    }
                                            }
                                        }
                                        break;
                                    }

                                case Channel.Unreliable:
                                    {
                                        byte[] data = message.ReadNext(out int length);
                                        internal_iCall(data, length, playerId, _peerEndPoint, channelMode, targetMode, opMode, IsServer);
                                        break;
                                    }
                            }
                        }
                        else
                            throw new Exception("[Atom] Send -> The bytesTransferred is less than 0.");
                    }
                }
                catch (ThreadAbortException) { }
                catch (ObjectDisposedException) { }
                catch (SocketException ex)
                {
                    if (ex.ErrorCode == 10004)
                        return;
                    if (!IsServer && ex.ErrorCode == 10054)
                        return;

                    AtomLogger.LogStacktrace(ex);
                }
                catch (Exception ex)
                {
                    AtomLogger.LogStacktrace(ex);
                }
            })
            {
                Name = "Atom_RecvThread",
                Priority = ThreadPriority.Highest,
                IsBackground = true,
            }.Start();
        }

        public Socket GetSocket() => _socket;
        private void ReturnId(int id) => _ids.Push(id, true);
        private bool GetAvailableId(out int id)
        {
            id = 0;
            if (_ids.Count > 0)
                id = _ids.Pull();
            return id != 0;
        }

        public void Close()
        {
            try
            {
                _cancelTokenSource.Cancel();
                _socket.Close();
            }
            finally
            {
                _cancelTokenSource.Dispose();
            }
        }
    }
}
#endif