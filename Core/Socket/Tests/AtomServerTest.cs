using System.Net;
using UnityEngine;

namespace Atom.Core.Tests
{
    public class AtomServerTest : AtomSocket
    {
        private void Awake()
        {
            Initialize(new IPEndPoint(IPAddress.Any, 5055));
        }

        protected override Message OnServerMessageCompleted(AtomStream stream, ushort playerId, EndPoint endPoint, Channel channelMode, Target targetMode, Operation opMode, AtomSocket udp)
        {
            switch (base.OnServerMessageCompleted(stream, playerId, endPoint, channelMode, targetMode, opMode, udp))
            {
                case Message.Test:
                    Debug.Log($"Server message: test");
                    break;
            }

            return default;
        }
    }
}