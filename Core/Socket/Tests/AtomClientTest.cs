using System.Net;
using UnityEngine;

namespace Atom.Core.Tests
{
    public class AtomClientTest : AtomSocket
    {
        private void Awake()
        {
            Initialize(new IPEndPoint(IPAddress.Any, 5058));
        }

        private void Start()
        {
            StartCoroutine(Connect(new IPEndPoint(IPAddress.Loopback, 5055)));
        }

        private void Update()
        {
            //if (Input.GetKey(KeyCode.Return))
            {
                using (AtomStream data = new())
                {
                    //data.Write((byte)Message.Test);
                    //SendToServer(data, Channel.Unreliable, Target.Single);
                }
            }
        }

        protected override Message OnClientMessageCompleted(AtomStream stream, ushort playerId, EndPoint endPoint, Channel channelMode, Target targetMode, Operation opMode, AtomSocket udp)
        {
            switch (base.OnClientMessageCompleted(stream, playerId, endPoint, channelMode, targetMode, opMode, udp))
            {
                case Message.Test:
                    Debug.Log($"Client message: test");
                    break;
            }

            return default;
        }
    }
}