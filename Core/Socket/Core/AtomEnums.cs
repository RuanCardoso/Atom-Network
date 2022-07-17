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
    Atom Enums is the structure responsible for assembling the enums to send to the network.
    ===========================================================*/

#if UNITY_2021_3_OR_NEWER
namespace Atom.Core
{
    /// <summary>Defines the channel used to send the message.</summary>
    public enum Channel : byte
    {
        ///<summary>It does not guarantee the message will be delivered or received in the same order.</summary>
        Unreliable = 0,
        /// <summary>Guarantees that the message will be delivered, but does not guarantee the order.</summary>
        Reliable = 1,
        /// <summary>Guarantees that the message will be delivered and will keep the same order.</summary>
        ReliableAndOrderly = 2
    }

    /// <summary>Defines the type of operation used to send the packet.</summary>
    public enum Operation : byte
    {
        /// <summary>Creates a message with a sequence number.</summary>
        Sequence = 0,
        /// <summary>Creates a message with a sequence number and data.</summary>
        Data = 1,
        /// <summary>Creates a message with a sequence number for acknowledgement.</summary>
        Acknowledgement = 2
    }

    /// <summary>
    /// Defines who will receive the message.</summary>
    public enum Target : byte
    {
        /// <summary>Send the message to all players.</summary>
        All,
        /// <summary>Send the message to all players, executes it immediately on this client.</summary>
        AllImmediately,
        /// <summary>Send the message to all players, except you.</summary>
        Others,
        /// <summary>Send the message only to yourself.</summary>
        Single,
        /// <summary>Send the message only to yourself.</summary>
        SingleImmediately,
        /// <summary>Send the message to the server only.</summary>
        Server
    }

    /// <summary>Defines the type of message to be sent.</summary>
    public enum Message : byte
    {
        ConnectAndPing = 1
    } // Zero is reserved for the default message.
}
#endif