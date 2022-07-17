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
using System;
using System.Diagnostics;

namespace Atom.Core
{
    public class AtomBandwidth
    {
        private readonly Stopwatch _stopwatch = new();
        private long _totalPackets;
        private double _lastSec;
        private long _totalBytes;

        public void Start()
        {
            // Before we receive the data, let's start the stopwatch.
            // This is used to calculate the bytes received per second.
            if (!_stopwatch.IsRunning)
                _stopwatch.Start();
        }

        public void Stop()
        {
            // After we receive the data, stop the stopwatch.
            // This is used to calculate the bytes received per second.
            if (_stopwatch.IsRunning)
                _stopwatch.Stop();
        }

        public void Add(int bytesTransferred)
        {
            _totalPackets++;
            _totalBytes += bytesTransferred;
        }

        public void Get(out int bytesRate, out int packetsRate)
        {
            bytesRate = 0;
            packetsRate = 0;

            double sec = _stopwatch.Elapsed.TotalSeconds;
            if (sec > 0)
            {
                // Let's calculate the bytes received per second and packets received per second.
                double bytesTransferRate = _totalBytes / sec;
                double packetsTransferRate = Math.Round(_totalPackets / sec);

                // If one second has passed, let's print the values.
                if (sec >= _lastSec + 1)
                {
                    bytesRate = (int)bytesTransferRate;
                    packetsRate = (int)packetsTransferRate;
                    // Set the last second to the current second.
                    _lastSec = sec;
                }

                // If 10 seconds has passed, let's reset the counters, to keep the good approximation.
                if (sec >= 10)
                {
                    _lastSec = _totalBytes = _totalPackets = 0;
                    _stopwatch.Reset();
                }
            }
        }
    }
}
#endif