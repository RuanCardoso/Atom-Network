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
    public class AtomBandwidth
    {
        private double _localTime;
        private double _totalMessages;
        private double _bytesTransferred;
        public double TotalMessages => Math.Round(_totalMessages / (AtomTime.LocalTime - _localTime));
        public double BytesTransferred => Math.Round(_bytesTransferred / (AtomTime.LocalTime - _localTime));

        public void Add(int bytesTransferred, double localTime)
        {
            if (_localTime == 0)
                _localTime = localTime;
            _totalMessages++;
            _bytesTransferred += bytesTransferred;
        }
    }
}
#endif