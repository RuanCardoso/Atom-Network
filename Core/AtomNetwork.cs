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

#if UNITY_2021_3_OR_NEWER
using Atom.Core.Interface;
using Atom.Core.Wrappers;
using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using static Atom.Core.AtomGlobal;

namespace Atom.Core
{
    [DefaultExecutionOrder(-5)]
    public class AtomNetwork : MonoBehaviour, ISocket
    {
        private static AtomNetwork _instance;
        private AtomSocket _server;
        private AtomSocket _client;

        private void Awake()
        {
            _instance = this;
#if UNITY_SERVER || UNITY_EDITOR
            _server = new(this);
            _server.Initialize("0.0.0.0", 5055);
#endif
#if !UNITY_SERVER || UNITY_EDITOR
            _client = new(this);
            _client.Initialize("0.0.0.0", GetFreePort());
#endif
        }

        void Start()
        {
            string[] address = Conf.Addresses[0].Split(':');
            _client.Connect(address[0], int.Parse(address[1]), this);
        }

#pragma warning disable IDE1006
        public static void gRPC(byte id, AtomStream writer, Channel channel = Channel.Unreliable, Target target = Target.Single, int playerId = 0)
        {
            int count = writer.CountBytes;
            byte[] data = writer.GetBuffer();
            writer.Reset(pos: sizeof(byte) * 2);
            data.Write(writer, 0, count);
            writer.Position = 0;
            ((byte)Message.gRPC).Write(writer);
            id.Write(writer);
            writer.Position = writer.CountBytes;
            if (playerId == 0) _instance._client.SendToServer(writer, channel, target);
            else _instance._server.SendToClient(writer, channel, target, playerId: playerId);
        }
#pragma warning restore IDE1006

        public void OnMessageCompleted(AtomStream reader, AtomStream writer, int playerId, EndPoint endPoint, Channel channel, Target target, Operation operation, bool isServer)
        {
            Message message = (Message)reader.ReadByte();
            if (isServer)
            {
                switch (message)
                {
                    case Message.gRPC:
                        byte id = reader.ReadByte();
                        if (AtomBehaviour.gRPCMethods.TryGetValue(id, out Action<AtomStream> gRPC))
                        {
                            AtomStream gRPCStream = AtomStream.Get();
                            byte[] data = reader.ReadNext(out int length);
                            gRPCStream.SetBuffer(data, 0, length);
                            gRPC(gRPCStream);
                        }
                        else
                            throw new Exception($"gRPC method with id: {id} -> not found.");
                        break;
                }
            }
        }

        public int GetFreePort()
        {
            UdpClient udpClient = new(new IPEndPoint(IPAddress.Any, 0));
            IPEndPoint endPoint = (IPEndPoint)udpClient.Client.LocalEndPoint;
            int port = endPoint.Port;
            udpClient.Close();
            return port;
        }

        private void OnApplicationQuit()
        {
            _client.Close();
            _server.Close();
        }
    }
}
#endif