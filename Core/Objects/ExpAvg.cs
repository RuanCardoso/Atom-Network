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

namespace Atom.Core.Objects
{
    public class ExpAvg
    {
        private bool _init;
        private double _alpha;

        public double Avg { get; private set; }
        public double Slope { get; private set; }

        public ExpAvg(int size) => _alpha = 2.0d / (size + 1);
        public void Increment(double value)
        {
            if (_init)
            {
                double delta = value - Avg;
                Avg += _alpha * delta;
                Slope = (1 - _alpha) * (Slope + _alpha * delta * delta);
            }
            else
            {
                if (!_init)
                {
                    Avg = value;
                    _init = true;
                }
            }
        }

        public void Reset(int size)
        {
            if (Avg > 0 && _init)
            {
                _init = false;
                _alpha = 2.0d / (size + 1);
            }
        }
    }
}
#endif