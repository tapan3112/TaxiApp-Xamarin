using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Xamarin.Forms.GoogleMaps.Android;
using Android.Support.V4.App;
using Android;
using System.Threading.Tasks;
using Java.Util;
using Android.Content.Res;

namespace taxiapp.Droid
{
    [Activity(Label = "taxiapp", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;
            Locale locale = Locale.English;
            Configuration config = new Configuration();
            config.Locale = locale;
            base.OnCreate(savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);

            var platformConfig = new PlatformConfig
            {
                BitmapDescriptorFactory = new CachingNativeBitmapDescriptorFactory()
            };

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            Xamarin.FormsGoogleMaps.Init(this, savedInstanceState, platformConfig);

            if (ActivityCompat.CheckSelfPermission(Application.Context, Manifest.Permission.AccessCoarseLocation) != Android.Content.PM.Permission.Granted 
            || ActivityCompat.CheckSelfPermission(Application.Context, Manifest.Permission.AccessFineLocation) != Android.Content.PM.Permission.Granted )
            {
                ActivityCompat.RequestPermissions(this, new string[]
                    {
                        Manifest.Permission.AccessCoarseLocation,
                        Manifest.Permission.AccessFineLocation,
                        Manifest.Permission.WriteExternalStorage,
                        Manifest.Permission.Internet,
                        Manifest.Permission.LocationHardware,
                    }, 1001);

            }

            do
            {
                if (ActivityCompat.CheckSelfPermission(Application.Context, Manifest.Permission.AccessCoarseLocation) == Android.Content.PM.Permission.Granted
            && ActivityCompat.CheckSelfPermission(Application.Context, Manifest.Permission.AccessFineLocation) == Android.Content.PM.Permission.Granted)
                    break;
                else
                    Task.Delay(1000);
            } while (true);

            LoadApplication(new App());
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            Plugin.Permissions.PermissionsImplementation.Current.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}