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
using static Atom.Core.AtomGlobal;

namespace Atom.Core
{
    public class AtomBandwidth
    {
        private double _localTime;
        private double _totalMessages;
        private double _bytesTransferred;
        public double TotalMessages => Math.Round(_totalMessages / (AtomTime.LocalTime - _localTime), MidpointRounding.AwayFromZero);
        public double BytesTransferred => Math.Round(_bytesTransferred / (AtomTime.LocalTime - _localTime), MidpointRounding.AwayFromZero);

        public void Add(int bytesTransferred, double localTime)
        {
            _totalMessages++;
            _bytesTransferred += bytesTransferred;
            if (_localTime == 0)
                _localTime = localTime;
            else
            {
                double time = localTime - _localTime;
                if (time >= Settings.BandwidthTimeout)
                {
                    _totalMessages = _bytesTransferred = 0;
                    _localTime = localTime;
                }
            }
        }
    }
}
#endif