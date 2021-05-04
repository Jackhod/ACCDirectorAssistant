using System;
using System.Collections.Generic;
using System.Text;

namespace ACCAssistedDirector.Core.Assistant {
    public class CarFeatures {
        public float Proximity { get; set; }
        public float CarsAroundMidRange { get; set; }
        public float CarsAroundCloseRange { get; set; }
        public float Pos { get; set; }
        public float Pace { get; set; }        
        public int SelectionID { get; set; }
        public int Selected { get; set; }

        public float[] ToArray() {
            return new float[] { Proximity, CarsAroundMidRange, CarsAroundCloseRange, Pos, Pace };
        }

        public float[] ToArrayLabeled() {
            return new float[] { Proximity, CarsAroundMidRange, CarsAroundCloseRange, Pos, Pace, SelectionID, Selected };
        }
    }
}
