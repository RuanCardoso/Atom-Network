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

namespace Atom.Core
{
    public class AtomRelayMessage
    {
        /// <summary>The message to be resent.</summary>
        public AtomMessage Data { get; }

        public AtomRelayMessage(AtomMessage data)
        {
            Data = data;
        }
    }
}
#endif