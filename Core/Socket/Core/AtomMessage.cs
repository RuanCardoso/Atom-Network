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
    Atom Message is the structure responsible for assembling the message to be sent to the remote host.
    ===========================================================*/

#if UNITY_2021_3_OR_NEWER
using System;
using System.Net;

namespace Atom.Core
{
    public class AtomMessage
    {
        /// <summary>Sequence number used to identify the message.</summary>
        public int SeqAck { get; }
        /// <summary>Id of the endpoint responsible for the message.</summary>
        public ushort PlayerId { get; set; }
        /// <summary>The endpoint responsible for the message.</summary>
        public EndPoint EndPoint { get; set; }
        /// <summary>The Channel used to send the message.</summary>
        public AtomChannel AtomChannel { get; }
        /// <summary>The data that will be sent with the message.</summary>
        public byte[] Data { get; }
        /// <summary> Defines whether it is a relay message.</summary>
        public bool IsRelay { get; set; }
        /// <summary>Last timestamp the packet was sent.</summary>
        public DateTime LastSent { get; set; }
        public Operation Operation { get; }
        public Target Target { get; set; }
        public Channel Channel { get; }
        /// <summary>Number of attempts the message was resent.</summary>
        public int Attempts = 0;

        ///<summary>Reliable constructor.</summary>
        public AtomMessage(int seqAck, ushort playerId, DateTime lastSent, EndPoint endPoint, Operation operationMode, Target targetMode, Channel channelMode, AtomChannel channelData, byte[] data)
        {
            PlayerId = playerId;
            SeqAck = seqAck;
            LastSent = lastSent;
            EndPoint = endPoint;
            Operation = operationMode;
            Target = targetMode;
            Channel = channelMode;
            AtomChannel = channelData;
            Data = data;
        }

        ///<summary> Unreliable constructor.</summary>
        public AtomMessage(ushort playerId, EndPoint endPoint, Operation operationMode, Target targetMode, Channel channelMode, byte[] data)
        {
            PlayerId = playerId;
            EndPoint = endPoint;
            Operation = operationMode;
            Target = targetMode;
            Channel = channelMode;
            Data = data;
        }

        public bool IsDisconnected()
        {
            return true;
            //// Thread safe: Simultaneous access to this value is not a problem.
            //int value = Interlocked.Increment(ref Attempts);
            //if (value >= 150)
            //    LogHelper.Info($"Packet with sequence: {SeqAck} was lost and was not re-transmitted ):");
            //return value >= 150;
        }
    }
}
#endif
