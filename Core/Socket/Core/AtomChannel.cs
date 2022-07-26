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
using System.Linq;

namespace Atom.Core
{
    public class AtomChannel
    {
        static int Syn { get; } = 0;
        public int SentAck = Syn;
        public ConcurrentDictionary<(int, int), AtomRelayMessage> MessagesToRelay = new();
        public HashSet<int> Acknowledgements = new();
        public SortedDictionary<int, byte[]> SequentialData = new();
        public int ExpectedAcknowledgement { get; set; } = Syn + 1;
    }
}
#endif