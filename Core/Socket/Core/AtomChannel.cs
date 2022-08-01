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
    Atom Channel is the structure responsible for the channel on which the messages will be transmitted.
    ===========================================================*/

#if UNITY_2021_3_OR_NEWER
using System.Collections.Concurrent;
using System.Collections.Generic;
using Atom.Core.Wrappers;

namespace Atom.Core
{
    public class AtomChannel
    {
        internal int sentAck = 0;
        internal ConcurrentDictionary<int, AtomStream> MessagesToRelay = new();
        internal SortedDictionary<int, byte[]> SequentialData = new();
        internal HashSet<int> Acks = new();
        internal int ExpectedAck { get; set; } = 1;
    }
}
#endif