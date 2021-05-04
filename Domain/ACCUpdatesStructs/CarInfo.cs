using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.ACCUpdatesStructs {
    public struct CarInfo
    {
        public ushort CarIndex { get; set; }
        public byte CarModelType { get; set; }
        public string TeamName { get; set; }
        public int RaceNumber { get; set; }
        public byte CupCategory { get; set; }
        public int CurrentDriverIndex { get; set; }
        public NationalityEnum Nationality { get; set; }
    }
}
