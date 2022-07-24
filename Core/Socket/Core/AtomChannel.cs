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
        /// <summary>Increments with each sending, defines the id of the message sequence.</summary>
        public int SentAck = Syn;
        /// <summary>This value will be used to compare if the first sequence number in the array is the same as the last one + 1.</summary>
        public int LastReceivedSequentialAck { get; set; } = Syn;
        /// <summary>This value returns the last sequence number processed.</summary>
        public int LastProcessedSequentialAck { get; set; } = Syn;
        /// <summary> List of the packets that are waiting to be re-sent.</summary>
        public ConcurrentDictionary<(int, ushort), AtomRelayMessage> MessagesToRelay = new();
        /// <summary>Any sequence is received is added to this list, It's only used to check if the sequence is already received.</summary>
        public HashSet<int> Acknowledgements = new();
        /// <summary> All messages are added to this list is automatically sorted by sequence number.</summary>
        public SortedDictionary<int, byte[]> SequentialData = new();
        /// <summary> This method is used to check if the all data is in sequence.</summary>
        public bool IsSequential()
        {
            // The next sequence number is the last sequence number + 1.
            // If the array length is 1, formula is: Array[0] == (LastReceivedSequentialAck + 1) == Ok, the first received packet is in sequence.
            int nextSequence = LastReceivedSequentialAck + 1;
            // Convert the dictionary keys to an array.
            // The dictionary keys are the sequence numbers.
            // Let's compare if it is in sequence using the following formula: array[index + 1] == array[index] + 1.
            int[] keys = SequentialData.Keys.ToArray();
            // If the length is zero, the data is not in sequence.
            // If the length is one, check: Array[0] == (LastReceivedSequentialAck + 1) == Ok, the first received packet is in sequence.
            // If the length is more than one, check: array[index + 1] == array[index] + 1.
            if (keys.Length == 0)
                return false;
            else if (keys.Length == 1)
                return keys[0] == nextSequence;
            else if (keys.Length > 1)
            {
                for (int i = 0; i < keys.Length; i++)
                {
                    // If the index is the last index, check: array[^1] == array[^2] + 1 == Ok, the last received packet is in sequence.
                    if (i + 1 == keys.Length)
                        return keys[^1] == keys[^2] + 1;
                    else
                    {
                        // If the index is not the last index, check: array[index + 1] == array[index] + 1 == Ok, the received packets are in sequence.
                        // If the last index, an exception will be thrown, because the last index + 1 is out of range.
                        if (keys[i + 1] != keys[i] + 1)
                            return false;
                        else
                            continue;
                    }
                }
            }
            return true;
        }
    }
}
#endif