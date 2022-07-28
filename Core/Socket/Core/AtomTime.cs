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
using Atom.Core.Objects;
using System;
using System.Diagnostics;

namespace Atom.Core
{
    public static class AtomTime
    {
        const int SIZE = 10;
        private static double _receivedMessages = 1d;
        private static double _messagesSent = 1d;
        private static double _offsetMin = double.MinValue;
        private static double _offsetMax = double.MaxValue;
        private static readonly ExpAvg _rttExAvg = new(SIZE);
        private static readonly ExpAvg _offsetExAvg = new(SIZE);

        public static double LostMessages => Math.Abs(Math.Round(100d - ((_receivedMessages / _messagesSent) * 100d), MidpointRounding.ToEven));
        public static double Latency => Math.Round((RoundTripTime * 0.5d) * 1000d);
        public static double RoundTripTime => _rttExAvg.Avg;
        public static double LocalTime => AtomCore.NetworkTime;
        public static double Time => LocalTime + (Offset * -1);
        public static double RttSlope => _rttExAvg.Slope;
        public static double OffsetSlope => _offsetExAvg.Slope;
        public static double Offset => _offsetExAvg.Avg;

        public static void SetTime(double clientTime, double serverTime)
        {
            double now = LocalTime;
            double rtt = now - clientTime;
            double halfRtt = rtt * 0.5d;
            double offset = now - halfRtt - serverTime;
            double offsetMin = now - rtt - serverTime;
            double offsetMax = now - serverTime;

            _offsetMin = Math.Max(_offsetMin, offsetMin);
            _offsetMax = Math.Min(_offsetMax, offsetMax);

            _rttExAvg.Increment(rtt);
            if (_offsetExAvg.Avg < _offsetMin || _offsetExAvg.Avg > _offsetMax)
            {
                _offsetExAvg.Reset(SIZE);
                _offsetExAvg.Increment(offset);
            }
            else if (offset >= _offsetMin || offset <= _offsetMax)
                _offsetExAvg.Increment(offset);
        }

        public static void AddSent() => _messagesSent++;
        public static void AddReceived() => _receivedMessages++;
    }
}
#endif