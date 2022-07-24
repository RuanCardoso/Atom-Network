using Atom.Core.Interface;
using Atom.Core.Wrappers;
using System.Net;
using UnityEngine;

namespace Atom.Core.Tests
{
    public class AtomServerTest : MonoBehaviour, ISocketServer
    {
        AtomSocket serverSocket = new();
        private void Awake()
        {
            serverSocket.Initialize(new IPEndPoint(IPAddress.Any, 5055), true);
        }

        private void Start()
        {
            serverSocket.OnMessageCompleted += OnServerMessageCompleted;
        }

        public void OnServerMessageCompleted(AtomStream reader, AtomStream writer, ushort playerId, EndPoint endPoint, Channel channelMode, Target targetMode, Operation opMode)
        {
            switch (serverSocket.OnServerMessageCompleted(reader, writer, playerId, endPoint, channelMode, targetMode, opMode))
            {
                case Message.Test:
                    Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "Message(Server): {0}", Message.Test);
                    serverSocket.SendToClient(reader, channelMode, targetMode, opMode, playerId);
                    break;
            }
        }

        private void OnApplicationQuit()
        {
            serverSocket.Close();
            Debug.Log("Server stopped!");
        }
    }
}