using Domain.ACCUpdatesStructs;
using Domain.Enums;
using System.Linq;

namespace Domain.Models {
    public class DriverModel {
        public string FirstName { get; private set; }
        public string LastName { get; private set; }
        public string ShortName { get; private set; }
        public string DisplayName { get; private set; }
        public DriverCategory Category { get; private set; }
        public NationalityEnum Nationality { get; set; }

        public DriverModel(DriverInfo driverInfo) {
            FirstName = driverInfo.FirstName;
            LastName = driverInfo.LastName;
            ShortName = driverInfo.ShortName;
            Category = driverInfo.Category;
            Nationality = driverInfo.Nationality;

            var displayName = $"{FirstName?.First()}. {LastName}".TrimStart('.').Trim();
            if (string.IsNullOrEmpty(displayName))
                displayName = "NO NAME";

            DisplayName = displayName;
        }
    }
}
