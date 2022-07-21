using Atom.Core.Wrappers;
using System.Net;
using UnityEngine;

namespace Atom.Core.Tests
{
    public class AtomClientTest : AtomSocket
    {
        private void Awake()
        {
            Initialize(new IPEndPoint(IPAddress.Any, Random.Range(5056, 5090)), false);
        }

        private void Start()
        {
            StartCoroutine(Connect(new IPEndPoint(IPAddress.Loopback, 5055)));
        }

        readonly AtomStream fixedStream = new(AtomCore.UnrealibleSize + sizeof(byte));
        private void Update()
        {
            //if (Input.GetKeyDown(KeyCode.Return))
            {
                for (int i = 0; i < 1200; i++)
                {
                    using (AtomStream data = fixedStream)
                    {
                        data.Write((byte)Message.Test);
                        SendToServer(data, Channel.Unreliable, Target.Single);
                    }
                }
            }
        }

        protected override Message OnClientMessageCompleted(AtomStream reader, AtomStream writer, ushort playerId, EndPoint endPoint, Channel channelMode, Target targetMode, Operation opMode, AtomSocket udp)
        {
            switch (base.OnClientMessageCompleted(reader, writer, playerId, endPoint, channelMode, targetMode, opMode, udp))
            {
                case Message.Test:
                    Debug.Log($"Client message: test");
                    break;
            }

            return default;
        }
    }
}