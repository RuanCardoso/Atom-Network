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
using Atom.Core.Wrappers;
using System.Net;

namespace Atom.Core.Interface
{
    public interface ISocketClient
    {
        void OnClientMessageCompleted(AtomStream reader, AtomStream writer, ushort playerId, EndPoint endPoint, Channel channel, Target target, Operation operation);
    }

    public interface ISocketServer
    {
        void OnServerMessageCompleted(AtomStream reader, AtomStream writer, ushort playerId, EndPoint endPoint, Channel channel, Target target, Operation operation);
    }
}
#endif