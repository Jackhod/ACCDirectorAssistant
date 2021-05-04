using Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace ACCAssistedDirector.Core.Assistant {
    public class CamFeatureVector {
        public int CarsAround { get; set; }
        public float GapRear { get; set; }
        public float GapFront { get; set; }
        public CamTypeEnum Label { get; set; }

        public float[] ToArray() {
            return new float[] { CarsAround, GapRear, GapFront };
        }
    }
}
