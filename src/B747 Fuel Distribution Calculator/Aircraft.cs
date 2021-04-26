using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B747_Fuel_Distribution_Calculator
{
    class Aircraft
    {
        public string AircraftName { get; private set; }
        public long MainTreshold14 { get; private set; }
        public long MainLimit14 { get; private set; }
        public long MainLimit23 { get; private set; }
        public long ReserveLimit14 { get; private set; }
        public long CenterLimit { get; private set; }
        public long StabLimit { get; private set; }
        public long CapacityLimit { get; private set; }
        public int[] Labels { get; private set; }

        public Aircraft(string AircraftName, long MainTreshold14, long MainLimit14, long MainLimit23, long ReserveLimit14, long CenterLimit, long StabLimit, long CapacityLimit, int[] Labels)
        {
            this.AircraftName = AircraftName;
            this.MainLimit14 = MainLimit14;
            this.MainLimit23 = MainLimit23;
            this.MainTreshold14 = MainTreshold14;
            this.ReserveLimit14 = ReserveLimit14;
            this.CenterLimit = CenterLimit;
            this.StabLimit = StabLimit;
            this.CapacityLimit = CapacityLimit;
            this.Labels = Labels;
        }
    }
}
