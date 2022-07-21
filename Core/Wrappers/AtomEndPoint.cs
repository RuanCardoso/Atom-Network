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
    Zero allocations: This structure is responsible for avoiding the allocation of a new EndPoint to each call.
    ===========================================================*/

#if UNITY_2021_3_OR_NEWER
using System;
using System.Net;
using System.Net.Sockets;

namespace Atom.Core.Wrappers
{
    public class AtomEndPoint : IPEndPoint
    {
        private SocketAddress _socketAddress;

        public AtomEndPoint(long address, int port) : base(address, port) => _socketAddress = base.Serialize();
        public AtomEndPoint(IPAddress address, int port) : base(address, port) => _socketAddress = base.Serialize();

        public override AddressFamily AddressFamily => AddressFamily.InterNetwork;
        public override SocketAddress Serialize() => _socketAddress;
        public override EndPoint Create(SocketAddress socketAddress)
        {
            if (socketAddress.Family != AddressFamily)
                throw new Exception("Invalid address family");
            if (socketAddress.Size < 8)
                throw new Exception("Error: SocketAddress.Size < 8");

            if (_socketAddress != socketAddress)
            {
                _socketAddress = socketAddress;

                unchecked
                {
                    _socketAddress[0] += 1;
                    _socketAddress[0] -= 1;
                }

                if (_socketAddress.GetHashCode() == 0)
                    throw new Exception("Error: SocketAddress.GetHashCode() == 0");
            }

            return this;
        }

        private long GetIPAddress()
        {
            switch (AddressFamily)
            {
                case AddressFamily.InterNetwork:
                    {
                        long address = (
                                _socketAddress[4] & 0x000000FF |
                                _socketAddress[5] << 8 & 0x0000FF00 |
                                _socketAddress[6] << 16 & 0x00FF0000 |
                                _socketAddress[7] << 24
                                ) & 0x00000000FFFFFFFF;
                        return address;
                    }
                default:
                    throw new SocketException((int)SocketError.AddressFamilyNotSupported);
            }
        }

        private int GetPort()
        {
            switch (AddressFamily)
            {
                case AddressFamily.InterNetwork:
                    {
                        int port = (
                                _socketAddress[2] << 8 & 0x0000FF00 |
                                _socketAddress[3]
                                ) & 0x0000FFFF;
                        return port;
                    }
                default:
                    throw new SocketException((int)SocketError.AddressFamilyNotSupported);
            }
        }

        public override int GetHashCode() => _socketAddress.GetHashCode();
        public override bool Equals(object obj) => obj is AtomEndPoint other && GetIPAddress() == other.GetIPAddress() && GetPort() == other.GetPort();
        public override string ToString()
        {
            long ipAddress = GetIPAddress();
            int port = GetPort();
            return $"{ipAddress}:{port}";
        }
    }
}
#endif