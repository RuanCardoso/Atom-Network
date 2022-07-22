using Atom.Core.Wrappers;
using System.Net;
using UnityEngine;

namespace Atom.Core.Tests
{
    public class AtomServerTest : AtomSocket
    {
        private void Awake()
        {
            Initialize(new IPEndPoint(IPAddress.Any, 5055), true);
        }

        private void Start()
        {

        }

        protected override Message OnServerMessageCompleted(AtomStream reader, AtomStream writer, ushort playerId, EndPoint endPoint, Channel channelMode, Target targetMode, Operation opMode, AtomSocket udp)
        {
            switch (base.OnServerMessageCompleted(reader, writer, playerId, endPoint, channelMode, targetMode, opMode, udp))
            {
                case Message.Test:
                    Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "Message: {0}", Message.Test);
                    break;
            }

            return default;
        }

        private void OnApplicationQuit()
        {
            Close();
            Debug.Log("Server stopped!");
        }
    }
}