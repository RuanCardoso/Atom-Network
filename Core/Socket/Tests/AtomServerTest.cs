using Atom.Core.Wrappers;
using System.Net;
using UnityEngine;

namespace Atom.Core.Tests
{
    public class AtomServerTest : AtomSocket
    {
#if UNITY_EDITOR
        private void Awake()
        {
            Initialize(new IPEndPoint(IPAddress.Any, 5055), true);
        }

        protected override Message OnServerMessageCompleted(AtomStream reader, AtomStream writer, ushort playerId, EndPoint endPoint, Channel channelMode, Target targetMode, Operation opMode, AtomSocket udp)
        {
            switch (base.OnServerMessageCompleted(reader, writer, playerId, endPoint, channelMode, targetMode, opMode, udp))
            {
                case Message.Test:
                    //Debug.Log($"Server message: test");
                    break;
            }

            return default;
        }
#endif
    }
}