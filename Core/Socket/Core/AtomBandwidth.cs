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
using System;
using System.Diagnostics;

namespace Atom.Core
{
    public class AtomBandwidth
    {
        private readonly Stopwatch _stopwatch = new();
        private double _lastSec;
        private long _totalMessages;
        private long _totalBytes;

        public void Start() // Before we receive the data, let's start the stopwatch.
        {
            if (!_stopwatch.IsRunning)
                _stopwatch.Start();
        }

        public void Stop() // After we receive the data, stop the stopwatch.
        {
            if (_stopwatch.IsRunning)
                _stopwatch.Stop();
        }

        public void Add(int bytesTransferred)
        {
            _totalMessages++;
            _totalBytes += bytesTransferred;
        }

        public void Get(out int bytesRate, out int messageRate)
        {
            bytesRate = messageRate = 0;
            double seconds = _stopwatch.Elapsed.TotalSeconds;
            if (seconds > 0)
            {
                double bytesTransferRate = _totalBytes / seconds;
                double packetsTransferRate = Math.Round(_totalMessages / seconds);

                if (seconds >= _lastSec + 1)
                {
                    bytesRate = (int)bytesTransferRate;
                    messageRate = (int)packetsTransferRate;
                    _lastSec = seconds;
                }

                if (seconds >= 5.016)
                {
                    _lastSec = _totalBytes = _totalMessages = 0;
                    _stopwatch.Reset();
                }
            }
        }
    }
}
#endif