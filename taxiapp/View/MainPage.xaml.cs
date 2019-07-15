using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using taxiapp.ViewModel;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.GoogleMaps;

namespace taxiapp.View
{
    public partial class MainPage : ContentPage
    {
        #region Bindable Properties and Commands
        MainPageViewModel vm;

        public static readonly BindableProperty CalculateCommandProperty =
            BindableProperty.Create(nameof(CalculateCommand), typeof(ICommand), typeof(MainPage), null, BindingMode.TwoWay);

        public ICommand CalculateCommand
        {
            get { return (ICommand)GetValue(CalculateCommandProperty); }
            set { SetValue(CalculateCommandProperty, value); }
        }

        public static readonly BindableProperty UpdateCommandProperty =
          BindableProperty.Create(nameof(UpdateCommand), typeof(ICommand), typeof(MainPage), null, BindingMode.TwoWay);

        public ICommand UpdateCommand
        {
            get { return (ICommand)GetValue(UpdateCommandProperty); }
            set { SetValue(UpdateCommandProperty, value); }
        }
        #endregion

        #region Constructor
        public MainPage()
        {
            InitializeComponent();
            this.mapn.UiSettings.MyLocationButtonEnabled = true;
            vm = new MainPageViewModel();
            this.BindingContext = vm;
            CalculateCommand = new Command<List<Xamarin.Forms.GoogleMaps.Position>>(Calculate);
            UpdateCommand = new Command<Xamarin.Forms.GoogleMaps.Position>(Update);
        }
        #endregion

        #region Methods
        async void Update(Xamarin.Forms.GoogleMaps.Position position)
        {
            if (mapn.Pins.Count == 1 && mapn.Polylines != null && mapn.Polylines?.Count > 1)
                return;
            var cPin = mapn.Pins.FirstOrDefault();
            if (cPin != null)
            {
                cPin.Position = new Position(position.Latitude, position.Longitude);
                cPin.Icon = BitmapDescriptorFactory.FromView(new Image() { Source = "ic_taxi.png", WidthRequest = 25, HeightRequest = 25 });

                await mapn.MoveCamera(CameraUpdateFactory.NewPosition(new Position(position.Latitude, position.Longitude)));
            }
        }

        void Calculate(List<Xamarin.Forms.GoogleMaps.Position> list)
        {
            mapn.Polylines.Clear();
            Polygon polygon = Getpolygondata();
            mapn.Polygons.Add(polygon);

            var polyline = new Xamarin.Forms.GoogleMaps.Polyline();
            polyline.StrokeColor = Color.Blue;
            polyline.StrokeWidth = polyline.StrokeWidth * 4;
            var polylineinside = new Xamarin.Forms.GoogleMaps.Polyline();
            polylineinside.StrokeColor = Color.Red;
            polylineinside.StrokeWidth = polyline.StrokeWidth * 2;

            foreach (var p in list)
            {
                polyline.Positions.Add(p);
                if (Contains(p, polygon))
                {
                    // point is inside polygon
                    polylineinside.Positions.Add(p);
                }
            }

            mapn.Polylines.Add(polyline);
            if (polylineinside.Positions.Count > 2)
                mapn.Polylines.Add(polylineinside);

            var pin = new Xamarin.Forms.GoogleMaps.Pin
            {
                Type = PinType.Place,
                Position = new Position(polyline.Positions.First().Latitude, polyline.Positions.First().Longitude),
                Label = vm.StartLabel,
                Address = vm.StartAdd,
                Tag = "First Point",
                Icon = BitmapDescriptorFactory.FromView(new Image() { Source = "ic_location.png", WidthRequest = 25, HeightRequest = 25 })
            };
            mapn.Pins.Add(pin);
            var pin1 = new Xamarin.Forms.GoogleMaps.Pin
            {
                Type = PinType.Place,
                Position = new Position(polyline.Positions.Last().Latitude, polyline.Positions.Last().Longitude),
                Label = vm.EndLabel,
                Address = vm.EndAdd,
                Tag = "Last Point",
                Icon = BitmapDescriptorFactory.FromView(new Image() { Source = "ic_location_red.png", WidthRequest = 25, HeightRequest = 25 })
            };
            mapn.Pins.Add(pin1);
            int InnerDist = 0;
            if (polylineinside.Positions.Count > 2)
            {
                var googleDirection = (vm.GetDistance(polylineinside.Positions.First(), polylineinside.Positions.Last()).Result);

                if (googleDirection.Routes != null && googleDirection.Routes.Count > 0)
                {
                    InnerDist = googleDirection.Routes.FirstOrDefault().Legs.FirstOrDefault().Distance.Value;
                }
            }
            int TotalDistance = vm.CurrentGoogleDirection.Routes.FirstOrDefault().Legs.FirstOrDefault().Distance.Value;
            decimal TotalFare = 0;
            decimal DistanceUnit = 0;
            decimal InnerDistanceUnit = 0;
            decimal OuterDistanceUnit = 0;
            int OuterDist = TotalDistance - InnerDist;
            decimal InnerFare = (Constants.InnerFareRate / (Constants.DistanceUnitKM / Constants.BillingUnit));
            decimal OuterFare = (Constants.OuterFareRate / (Constants.DistanceUnitKM / Constants.BillingUnit));
            if (InnerDist > 0)
            {
                InnerDistanceUnit = (Convert.ToDecimal(InnerDist) / Constants.BillingUnit);
                OuterDistanceUnit = (Convert.ToDecimal(OuterDist) / Constants.BillingUnit);

                TotalFare = (InnerFare * InnerDistanceUnit) + (OuterFare * OuterDistanceUnit);
                DistanceUnit = InnerDistanceUnit + OuterDistanceUnit;
            }
            else
            {
                DistanceUnit = (TotalDistance / Constants.BillingUnit);
                TotalFare = (OuterFare * DistanceUnit);
            }


            mapn.MoveToRegion(MapSpan.FromPositions(list));

            App.Current.MainPage.DisplayAlert("Price", "Total Distance : " + TotalDistance + " & Total Price : " + TotalFare.ToString("F2")
                + "\nOuter Distance : " + OuterDist + " @ Price : " + Constants.OuterFareRate + " cents"
                + "\nInner Distance : " + InnerDist + " @ Price : " + Constants.InnerFareRate + " cents", "Ok");

        }

        private async void OnEnterAddressTapped(object sender, EventArgs e)
        {
            await Navigation.PushModalAsync(new SearchPlacePage(vm) { BindingContext = this.BindingContext }, true);
        }

        bool Contains(Position point, Polygon p)
        {
            var crossings = 0;
            List<Position> path = p.Positions.ToList();
            int j;
            // for each edge
            for (var i = 0; i < path.Count; i++)
            {
                var a = path[i];
                j = i + 1;
                if (j >= path.Count)
                {
                    j = 0;
                }
                var b = path[j];
                if (RayCrossesSegment(point, a, b))
                {
                    crossings++;
                }
            }

            // odd number of crossings?
            return (crossings % 2 == 1);
        }

        bool RayCrossesSegment(Position point, Position a, Position b)
        {
            var px = point.Longitude;
            var py = point.Latitude;
            var ax = a.Longitude;
            var ay = a.Latitude;
            var bx = b.Longitude;
            var by = b.Latitude;
            if (ay > by)
            {
                ax = b.Longitude;
                ay = b.Latitude;
                bx = a.Longitude;
                by = a.Latitude;
            }
            // alter longitude to cater for 180 degree crossings
            if (px < 0)
            {
                px += 360;
            }
            if (ax < 0)
            {
                ax += 360;
            }
            if (bx < 0)
            {
                bx += 360;
            }

            if (py == ay || py == by) py += 0.00000001;
            if ((py > by || py < ay) || (px > Math.Max(ax, bx))) return false;
            if (px < Math.Min(ax, bx)) return true;

            var red = (ax != bx) ? ((by - ay) / (bx - ax)) : 0;
            var blue = (ax != px) ? ((py - ay) / (px - ax)) : 0;
            return (blue >= red);
        }
       
        Polygon Getpolygondata()
        {
            var polygon = new Xamarin.Forms.GoogleMaps.Polygon();
            polygon.Positions.Add(new Position(41.1389019841448d, -8.61560349288011d));
            polygon.Positions.Add(new Position(41.1434744495776d, -8.67074812896517d));
            polygon.Positions.Add(new Position(41.1449838609599d, -8.67905202242122d));
            polygon.Positions.Add(new Position(41.1451299199957d, -8.67927438576518d));
            polygon.Positions.Add(new Position(41.1667491884192d, -8.69051961393614d));
            polygon.Positions.Add(new Position(41.1687515832633d, -8.69128901091482d));
            polygon.Positions.Add(new Position(41.1687718202548d, -8.69129406942044d));
            polygon.Positions.Add(new Position(41.1733985483824d, -8.6901233398917d));
            polygon.Positions.Add(new Position(41.1791950462077d, -8.66638025932549d));
            polygon.Positions.Add(new Position(41.1843186499419d, -8.64263275539779d));
            polygon.Positions.Add(new Position(41.1859199810206d, -8.60475022412726d));
            polygon.Positions.Add(new Position(41.1859353051989d, -8.60415557268555d));
            polygon.Positions.Add(new Position(41.1858564905415d, -8.60352615847566d));
            polygon.Positions.Add(new Position(41.1809534888858d, -8.57618203495341d));
            polygon.Positions.Add(new Position(41.1808879776888d, -8.57596086663742d));
            polygon.Positions.Add(new Position(41.1782338392238d, -8.56918812805673d));
            polygon.Positions.Add(new Position(41.1781107344154d, -8.5690029395442d));
            polygon.Positions.Add(new Position(41.1778608190979d, -8.56863743584061d));
            polygon.Positions.Add(new Position(41.1776820677363d, -8.56838121049044d));
            polygon.Positions.Add(new Position(41.1772845426276d, -8.56783274898752d));
            polygon.Positions.Add(new Position(41.1761404097255d, -8.56630188826643d));
            polygon.Positions.Add(new Position(41.1760550093552d, -8.56619263815834d));
            polygon.Positions.Add(new Position(41.1759695143603d, -8.56610877045314d));
            polygon.Positions.Add(new Position(41.1600578943756d, -8.55337925107515d));
            polygon.Positions.Add(new Position(41.158207085713d, -8.55261345505833d));
            polygon.Positions.Add(new Position(41.1532077014578d, -8.55473745611279d));
            polygon.Positions.Add(new Position(41.1529458174769d, -8.55494508426352d));
            polygon.Positions.Add(new Position(41.1463677636723d, -8.56368877972558d));
            polygon.Positions.Add(new Position(41.1399578511665d, -8.57576315330342d));
            polygon.Positions.Add(new Position(41.1383506797128d, -8.59418349674972d));

            polygon.IsClickable = true;
            polygon.StrokeColor = Color.Black;
            polygon.StrokeWidth = 3f;
            polygon.FillColor = Color.FromRgba(255, 0, 0, 64);
            polygon.Tag = "POLYGON";
            return polygon;
        }
        #endregion
    }
}
