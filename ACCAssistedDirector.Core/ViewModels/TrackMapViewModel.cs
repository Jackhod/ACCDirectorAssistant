using ACCAssistedDirector.Core.Assistant;
using ACCAssistedDirector.Core.Assistant.Interfaces;
using ACCAssistedDirector.Core.Services;
using Domain.Models;
using MvvmCross.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ACCAssistedDirector.Core.ViewModels {
    public class TrackMapViewModel : MvxViewModel {
        
        private MvxObservableCollection<Point> _points = new MvxObservableCollection<Point>();
        public MvxObservableCollection<Point> Points
        {
            get { return _points; }
            set { SetProperty(ref _points, value); }
        }

        private bool _radioButtonSelection = true; //true: Lap View, false: Gap View
        public bool RadioButtonSelection
        {
            get { return _radioButtonSelection; }
            set 
            { 
                SetProperty(ref _radioButtonSelection, value);
                UpdateRelativeCarIconsPos();
                foreach (var p in _points) p.SetCoordinates(p.SplinePos);
            }
        }

        private float _length;
        public float Length
        {
            get { return _length; }
            set { SetProperty(ref _length, value); }
        }

        private IDirectorAssistant directorAssistant;
        private ICarEntryListService carEntryListService;

        public TrackMapViewModel(IDirectorAssistant directorAssistant, ICarEntryListService carEntryListService) {
            this.directorAssistant = directorAssistant;
            this.carEntryListService = carEntryListService;
            directorAssistant.OnNewTipsGenerated += OnNewTipsGenerated;
            carEntryListService.OnEntryListUpdated += AddCarIcon;
            carEntryListService.OnCarEntryUpdated += UpdateCarIcon;
            carEntryListService.OnLastCarUpdated += UpdateRelativeCarIconsPos;
        }

        private void OnNewTipsGenerated(List<DirectorTipModel> directorTips) {
            foreach (var icon in Points) icon.Selected = false;
            foreach (var tip in directorTips) {
                var carIcon = Points.FirstOrDefault(p => p.CarIndex == tip.CarTip.Tip.CarInfo.CarIndex);
                carIcon.Selected = true;
            }
        }

        private void AddCarIcon(CarUpdateModel carUpdateModel) {
            Points.Add(new Point() {
                TrackMapVM = this,
                SplinePos = 0.0f,
                RaceNumber = carUpdateModel.CarInfo.RaceNumber,
                CarIndex = carUpdateModel.CarInfo.CarIndex               
            });
        }

        private void UpdateRelativeCarIconsPos() {

            if (RadioButtonSelection == true) return;

            float maxPos = 0;
            float minPos = float.MaxValue;

            // we find the max position
            foreach(var car in carEntryListService.CarEntryList) {
                var carIcon = _points.FirstOrDefault(p => p.CarIndex == car.CarInfo.CarIndex);
                carIcon.Y = car.SplinePosition + car.Laps;
                if (carIcon.Y > maxPos) maxPos = Convert.ToSingle(carIcon.Y);
            }

            //we find the minimum position that is less than one lap apart from the maximum position
            foreach(var p in _points) {
                if (maxPos-p.Y <= 1 && p.Y < minPos) minPos = Convert.ToSingle(p.Y);
            }

            var factor = 1 / (maxPos - minPos);
            foreach(var p in _points) {

                if (p.Y < minPos) p.Y = minPos;

                p.Y = 1 - (p.Y - minPos) * factor;
                p.Y *= Length;
                p.LabelY = p.Y + 2;
            }
        }

        private void UpdateCarIcon(int carIndex) {
            var carIcon = _points.FirstOrDefault(p => p.CarIndex == carIndex);
            if (carIcon != null) {
                var car = carEntryListService.GetCarById(carIndex);
                carIcon.SplinePos = car.SplinePosition;
                carIcon.ZIndex = -car.TrackPosition;
            }
        }

        public class Point : MvxViewModel {

            private float _splinePos;
            public float SplinePos
            {
                get { return _splinePos; }
                set
                {
                    _splinePos = value;
                    SetCoordinates(value);
                }
            }

            private int _posX; // -1:left 0:central 1:right
            public int PosX
            {
                get { return _posX; }
                set { SetProperty(ref _posX, value); }
            }

            private double _y;
            public double Y
            {
                get { return _y; }
                set { SetProperty(ref _y, value); }
            }

            private double _labelY;
            public double LabelY
            {
                get { return _labelY; }
                set { SetProperty(ref _labelY, value); }
            }

            private int _zIndex;
            public int ZIndex
            {
                get { return _zIndex; }
                set { SetProperty(ref _zIndex, value); }
            }

            private bool _selected;
            public bool Selected
            {
                get { return _selected; }
                set { SetProperty(ref _selected, value); }
            }

            private bool _leftLine;
            public bool LeftLine
            {
                get { return _leftLine; }
                set { SetProperty(ref _leftLine, value); }
            }          

            public int Laps { get; set; }
            public int RaceNumber { get; set; }
            public int CarIndex { get; set; }
            public TrackMapViewModel TrackMapVM { get; set; }

            public void SetCoordinates(float splinePos) {

                if (TrackMapVM.RadioButtonSelection == true) {
                    if (splinePos >= 0.5f) {
                        LeftLine = true;
                        PosX = -1;
                        Y = ((1 - splinePos) / 0.5f) * TrackMapVM.Length - 6;
                        LabelY = Y + 2;
                    } else {
                        LeftLine = false;
                        PosX = 1;
                        Y = (1 - ((0.5 - splinePos) / 0.5f)) * TrackMapVM.Length - 6;
                        LabelY = Y + 2;
                    }
                }

                if(TrackMapVM.RadioButtonSelection == false) PosX = 0;
            }
        }
    }
}
