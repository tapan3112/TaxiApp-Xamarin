using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using taxiapp.Models;
using taxiapp.Services;
using Xamarin.Forms;
using Xamarin.Forms.GoogleMaps;
using System.ComponentModel;
using taxiapp.Helpers;
using Xamarin.Essentials;

namespace taxiapp.ViewModel
{
    public class MainPageViewModel : NotifyPropertyChanged
    {

        #region Command
        public ICommand CalculateRouteCommand { get; set; }
        public ICommand UpdatePositionCommand { get; set; }
        public ICommand LoadRouteCommand { get; set; }
        public ICommand StopRouteCommand { get; set; }
        public ICommand FocusOriginCommand { get; set; }
        public ICommand GetPlacesCommand { get; set; }
        public ICommand GetPlaceDetailCommand { get; set; }
        public ICommand GetLocationNameCommand { get; set; }
        #endregion

        #region Properties and Api initialize

        IGoogleMapsApiService googleMapsApi = new GoogleMapsApiService();

        public bool HasRouteRunning { get; set; }
        string _originLatitud;
        string _originLongitud;
        string _destinationLatitud;
        string _destinationLongitud;

        GooglePlaceAutoCompletePrediction _placeSelected;
        public GooglePlaceAutoCompletePrediction PlaceSelected
        {
            get
            {
                return _placeSelected;
            }
            set
            {
                _placeSelected = value;
                RaiseNotifyPropertyChange();
                if (_placeSelected != null)
                    GetPlaceDetailCommand.Execute(_placeSelected);
            }
        }

        public MainPageViewModel mainVM { get; set; }

        private ObservableCollection<GooglePlaceAutoCompletePrediction> _Places;

        public ObservableCollection<GooglePlaceAutoCompletePrediction> Places
        {
            get { return _Places; }
            set { _Places = value; RaiseNotifyPropertyChange(); }
        }

        private ObservableCollection<GooglePlaceAutoCompletePrediction> _RecentPlaces = new ObservableCollection<GooglePlaceAutoCompletePrediction>();

        public ObservableCollection<GooglePlaceAutoCompletePrediction> RecentPlaces
        {
            get { return _RecentPlaces; }
            set { _RecentPlaces = value; RaiseNotifyPropertyChange(); }
        }
        private bool _ShowRecentPlaces;

        public bool ShowRecentPlaces
        {
            get { return _ShowRecentPlaces; }
            set { _ShowRecentPlaces = value; RaiseNotifyPropertyChange(); }
        }

        private bool _isPickupFocusedn;

        public bool _isPickupFocused
        {
            get { return _isPickupFocusedn; }
            set { _isPickupFocusedn = value; RaiseNotifyPropertyChange(); }
        }

        string _pickupText;
        public string PickupText
        {
            get
            {
                return _pickupText;
            }
            set
            {
                _pickupText = value;
                RaiseNotifyPropertyChange();
                if (!string.IsNullOrEmpty(_pickupText))
                {
                    _isPickupFocused = true;
                    GetPlacesCommand.Execute(_pickupText);
                }
            }
        }

        string _originText;
        public string OriginText
        {
            get
            {
                return _originText;
            }
            set
            {
                _originText = value;
                RaiseNotifyPropertyChange();
                if (!string.IsNullOrEmpty(_originText))
                {
                    _isPickupFocused = false;
                    GetPlacesCommand.Execute(_originText);
                }
            }
        }

        private string _startLabel;

        public string startLabel
        {
            get { return _startLabel; }
            set { _startLabel = value; RaiseNotifyPropertyChange(); }
        }

        private string _startAdd;

        public string startAdd
        {
            get { return _startAdd; }
            set { _startAdd = value; RaiseNotifyPropertyChange(); }
        }

        private string _endLabel;

        public string endLabel
        {
            get { return _endLabel; }
            set { _endLabel = value; RaiseNotifyPropertyChange(); }
        }

        private string _endAdd;

        public string endAdd
        {
            get { return _endAdd; }
            set { _endAdd = value; RaiseNotifyPropertyChange(); }
        }

        private GoogleDirection _currentGoogleDirection;

        public GoogleDirection CurrentGoogleDirection
        {
            get { return _currentGoogleDirection; }
            set { _currentGoogleDirection = value; RaiseNotifyPropertyChange(); }
        }

        private Entry _destEntry;

        public Entry destEntry
        {
            get { return _destEntry; }
            set { _destEntry = value; RaiseNotifyPropertyChange(); }
        }

        public bool IsRouteNotRunning
        {
            get
            {
                return !HasRouteRunning;
            }
        }
        #endregion

        #region Constructor
        public MainPageViewModel()
        {
            mainVM = this;
            LoadRouteCommand = new Command(async () => await LoadRoute());
            StopRouteCommand = new Command(StopRoute);
            GetPlacesCommand = new Command<string>(async (param) => await GetPlacesByName(param));
            GetPlaceDetailCommand = new Command<GooglePlaceAutoCompletePrediction>(async (param) => await GetPlacesDetail(param));
            GetLocationNameCommand = new Command<Position>(async (param) => await GetLocationName(param));
        }
        #endregion

        #region Method
        public async Task LoadRoute()
        {
            var googleDirection = await googleMapsApi.GetDirections(_originLatitud, _originLongitud, _destinationLatitud, _destinationLongitud);
            /* For Displaying Request and Response on Screen */
            await App.Current.MainPage.DisplayAlert("GoogleDirection", "https://maps.googleapis.com/maps/" + Constants.jsoncallstring, "OK");
            await App.Current.MainPage.DisplayAlert("GoogleDirection", Constants.jsonstring, "OK");

            Constants.jsoncallstring = "";
            Constants.jsonstring = "";
            if (googleDirection.Routes != null && googleDirection.Routes.Count > 0)
            {
                CurrentGoogleDirection = googleDirection;
                var positions = (Enumerable.ToList(PolylineHelper.Decode(googleDirection.Routes.First().OverviewPolyline.Points)));
                CalculateRouteCommand.Execute(positions);

                HasRouteRunning = true;
            }
            else
            {
                await App.Current.MainPage.DisplayAlert(":(", "No route found", "Ok");
            }

        }

        public async Task<GoogleDirection> GetDistance(Position First, Position Last)
        {
            var googleDirection = await googleMapsApi.GetDirections(First.Latitude.ToString(), First.Longitude.ToString(), Last.Latitude.ToString(), Last.Longitude.ToString()).ConfigureAwait(false);
            return googleDirection;
        }

        public void StopRoute()
        {
            HasRouteRunning = false;
        }

        public async Task GetPlacesByName(string placeText)
        {
            try
            {
                var places = await googleMapsApi.GetPlaces(placeText);
                var placeResult = places.AutoCompletePlaces;
                if (placeResult != null && placeResult.Count > 0)
                {
                    Places = new ObservableCollection<GooglePlaceAutoCompletePrediction>(placeResult);
                }

                ShowRecentPlaces = (placeResult == null || placeResult.Count == 0);
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task GetPlacesDetail(GooglePlaceAutoCompletePrediction placeA)
        {
            try
            {
                var place = await googleMapsApi.GetPlaceDetails(placeA.PlaceId);
                if (place != null)
                {
                    if (_isPickupFocused)
                    {
                        PickupText = place.Name;
                        startLabel = placeA.StructuredFormatting.MainText;
                        startAdd = placeA.StructuredFormatting.SecondaryText;
                        _originLatitud = $"{place.Latitude}";
                        _originLongitud = $"{place.Longitude}";
                        _isPickupFocused = false;
                        destEntry.Focus();
                    }
                    else
                    {
                        OriginText = place.Name;
                        endLabel = placeA.StructuredFormatting.MainText;
                        endAdd = placeA.StructuredFormatting.SecondaryText;

                        _destinationLatitud = $"{place.Latitude}";
                        _destinationLongitud = $"{place.Longitude}";

                        if (_originLatitud == _destinationLatitud && _originLongitud == _destinationLongitud)
                        {
                            await App.Current.MainPage.DisplayAlert("Error", "Origin route should be different than destination route", "Ok");
                        }
                        else
                        {
                            LoadRouteCommand.Execute(null);
                            await App.Current.MainPage.Navigation.PopModalAsync(true);
                            CleanFields();
                        }

                    }
                }
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        void CleanFields()
        {
            PickupText = OriginText = string.Empty;
            ShowRecentPlaces = true;
            PlaceSelected = null;
        }

        public async Task GetLocationName(Position position)
        {
            try
            {
                var placemarks = await Geocoding.GetPlacemarksAsync(position.Latitude, position.Longitude);
                var placemark = placemarks?.FirstOrDefault();
                if (placemark != null)
                {
                    PickupText = placemark.FeatureName;
                }
                else
                {
                    PickupText = string.Empty;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }
        #endregion
    }
}
