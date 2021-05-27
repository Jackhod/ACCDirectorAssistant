using Application.Services.Interfaces;
using Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace ACCAssistedDirector.Core.Services {

    public delegate void EntryListUpdatedDelegate(CarUpdateModel car);
    public delegate void CarEntryUpdatedDelegate(int carIndex);
    public delegate void LastCarUpdatedDelegate();
    public delegate void FocusedCarUpdatedDelegate(bool isAutoDirector);

    public interface ICarEntryListService : Service {
        public List<CarUpdateModel> CarEntryList { get; set; }
        public DateTime LastFocusChange { get; }
        public CarUpdateModel GetFocusedCar();
        public CarUpdateModel GetCarById(int carId);

        public event EntryListUpdatedDelegate OnEntryListUpdated;
        public event CarEntryUpdatedDelegate OnCarEntryUpdated;
        public event LastCarUpdatedDelegate OnLastCarUpdated;
        public event FocusedCarUpdatedDelegate OnFocusedCarUpdated;
    }
}
