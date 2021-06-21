using MvvmCross.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace ACCAssistedDirector.Core.ViewModels {
    public class ComboBoxItemViewModel<T> : MvxViewModel {

        private string _displayName;

        public string DisplayName
        {
            get { return _displayName; }
            set { if (value != _displayName) SetProperty(ref _displayName, value); }
        }

        private T _item;
        public T Item
        {
            get { return _item; }
            set {  SetProperty(ref _item, value); }
        }

        public ComboBoxItemViewModel(T item,  string displayName) {
            Item = item;
            DisplayName = displayName;
        }
    }
}
