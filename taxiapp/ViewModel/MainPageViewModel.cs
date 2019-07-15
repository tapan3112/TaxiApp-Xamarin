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
        string OriginLatitud;
        string OriginLongitud;
        string DestinationLatitud;
        string DestinationLongitud;

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

        public MainPageViewModel MainVM { get; set; }

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

        public bool IsPickupFocused
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
                    IsPickupFocused = true;
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
                    IsPickupFocused = false;
                    GetPlacesCommand.Execute(_originText);
                }
            }
        }

        private string _startLabel;

        public string StartLabel
        {
            get { return _startLabel; }
            set { _startLabel = value; RaiseNotifyPropertyChange(); }
        }

        private string _startAdd;

        public string StartAdd
        {
            get { return _startAdd; }
            set { _startAdd = value; RaiseNotifyPropertyChange(); }
        }

        private string _endLabel;

        public string EndLabel
        {
            get { return _endLabel; }
            set { _endLabel = value; RaiseNotifyPropertyChange(); }
        }

        private string _endAdd;

        public string EndAdd
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

        public Entry DestEntry
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
            MainVM = this;
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
            var googleDirection = await googleMapsApi.GetDirections(OriginLatitud, OriginLongitud, DestinationLatitud, DestinationLongitud);
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

        public async Task GetPlacesByName(string PlaceText)
        {
            try
            {
                var Placess = await googleMapsApi.GetPlaces(PlaceText);
                var PlaceResult = Placess.AutoCompletePlaces;
                if (PlaceResult != null && PlaceResult.Count > 0)
                {
                    Places = new ObservableCollection<GooglePlaceAutoCompletePrediction>(PlaceResult);
                }

                ShowRecentPlaces = (PlaceResult == null || PlaceResult.Count == 0);
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task GetPlacesDetail(GooglePlaceAutoCompletePrediction PlaceA)
        {
            try
            {
                var Place = await googleMapsApi.GetPlaceDetails(PlaceA.PlaceId);
                if (Place != null)
                {
                    if (IsPickupFocused)
                    {
                        PickupText = Place.Name;
                        StartLabel = PlaceA.StructuredFormatting.MainText;
                        StartAdd = PlaceA.StructuredFormatting.SecondaryText;
                        OriginLatitud = $"{Place.Latitude}";
                        OriginLongitud = $"{Place.Longitude}";
                        IsPickupFocused = false;
                        DestEntry.Focus();
                    }
                    else
                    {
                        OriginText = Place.Name;
                        EndLabel = PlaceA.StructuredFormatting.MainText;
                        EndAdd = PlaceA.StructuredFormatting.SecondaryText;

                        DestinationLatitud = $"{Place.Latitude}";
                        DestinationLongitud = $"{Place.Longitude}";

                        if (OriginLatitud == DestinationLatitud && OriginLongitud == DestinationLongitud)
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
                var Placemarks = await Geocoding.GetPlacemarksAsync(position.Latitude, position.Longitude);
                var Placemark = Placemarks?.FirstOrDefault();
                if (Placemark != null)
                {
                    PickupText = Placemark.FeatureName;
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
