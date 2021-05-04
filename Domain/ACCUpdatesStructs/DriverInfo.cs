using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.ACCUpdatesStructs {
    public struct DriverInfo
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string ShortName { get; set; }
        public DriverCategory Category { get; set; }
        public NationalityEnum Nationality { get; set; }
    }
}
