using Atom.Core.Interface;
using Atom.Core.Wrappers;
using System.Net;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Atom.Core.Tests
{
    public class AtomClientTest : MonoBehaviour, ISocketClient
    {
        readonly AtomSocket clientSocket = new();
        private void Awake()
        {
            clientSocket.Initialize(new IPEndPoint(IPAddress.Any, Random.Range(5056, 5090)), false);
        }

        private void Start()
        {
            StartCoroutine(clientSocket.Connect(new IPEndPoint(IPAddress.Loopback, 5055)));

            clientSocket.OnMessageCompleted += OnClientMessageCompleted;
        }

        readonly AtomStream fixedStream = new(AtomCore.RealibleSize + sizeof(byte));
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                for (int i = 0; i < 100; i++)
                {
                    using (AtomStream data = fixedStream)
                    {
                        data.Write((byte)Message.Test);
                        clientSocket.SendToServer(data, Channel.Reliable, Target.All);
                    }
                }
            }
        }

        public void OnClientMessageCompleted(AtomStream reader, AtomStream writer, ushort playerId, EndPoint endPoint, Channel channelMode, Target targetMode, Operation opMode)
        {
            switch (clientSocket.OnClientMessageCompleted(reader, writer, playerId, endPoint, channelMode, targetMode, opMode))
            {
                case Message.Test:
                    Debug.LogFormat(LogType.Error, LogOption.NoStacktrace, null, "Message(Client): {0} {1}", "Client", playerId);
                    break;
            }
        }

        private void OnApplicationQuit()
        {
            clientSocket.Close();
            Debug.Log("Client stopped!");
        }
    }
}