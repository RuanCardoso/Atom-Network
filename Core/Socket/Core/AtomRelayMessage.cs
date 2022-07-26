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
    Atom Relay Message is the structure responsible for assembling the relay message.
    ===========================================================*/

#if UNITY_2021_3_OR_NEWER
using System.Net;

namespace Atom.Core
{
    public class AtomRelayMessage
    {
        public AtomRelayMessage(int id, byte[] data, EndPoint endPoint)
        {
            Id = id;
            Data = data;
            EndPoint = endPoint;
        }

        public int Id { get; }
        public byte[] Data { get; }
        public EndPoint EndPoint { get; }
    }
}
#endif