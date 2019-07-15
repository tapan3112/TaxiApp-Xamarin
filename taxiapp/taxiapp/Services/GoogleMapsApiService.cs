using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using taxiapp.Models;
using Xamarin.Forms;

namespace taxiapp.Services
{
    public class GoogleMapsApiService : IGoogleMapsApiService
    {
        static string _googleMapsKey;

        private const string ApiBaseAddress = "https://maps.googleapis.com/maps/";
        private HttpClient CreateClient()
        {
            var httpClient = new HttpClient
            {
                BaseAddress = new Uri(ApiBaseAddress)
            };

            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            return httpClient;
        }
        public static void Initialize(string googleMapsKey)
        {
            _googleMapsKey = googleMapsKey;
        }

        public async Task<GoogleDirection> GetDirections(string originLatitude, string originLongitude, string destinationLatitude, string destinationLongitude)
        {
            try
            {
                GoogleDirection googleDirection = new GoogleDirection();
                using (var httpClient = CreateClient())
                {
                    originLatitude = originLatitude.Replace(',', '.');
                    originLongitude = originLongitude.Replace(',', '.');
                    destinationLatitude = destinationLatitude.Replace(',', '.');
                    destinationLongitude = destinationLongitude.Replace(',', '.');

                    Constants.jsoncallstring = $"api/directions/json?origin={originLatitude},{originLongitude}&destination={destinationLatitude},{destinationLongitude}&region='pt-PT'&key={_googleMapsKey}";
                    var response = await httpClient.GetAsync(Constants.jsoncallstring).ConfigureAwait(false);
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        if (!string.IsNullOrWhiteSpace(json))
                        {
                            Constants.jsonstring = json.ToString();
                            googleDirection = await Task.Run(() =>
                               JsonConvert.DeserializeObject<GoogleDirection>(json)
                            ).ConfigureAwait(false);

                        }
                    }
                }

                return googleDirection;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<GooglePlaceAutoCompleteResult> GetPlaces(string text)
        {
            try
            {
                GooglePlaceAutoCompleteResult results = null;

                using (var httpClient = CreateClient())
                {
                    var response = await httpClient.GetAsync($"api/place/autocomplete/json?input={Uri.EscapeUriString(text)}&key={_googleMapsKey}").ConfigureAwait(false);
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        if (!string.IsNullOrWhiteSpace(json) && json != "ERROR")
                        {
                            results = await Task.Run(() =>
                               JsonConvert.DeserializeObject<GooglePlaceAutoCompleteResult>(json)
                            ).ConfigureAwait(false);

                        }
                    }
                }

                return results;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<GooglePlace> GetPlaceDetails(string placeId)
        {
            try
            {
                GooglePlace result = null;
                using (var httpClient = CreateClient())
                {
                    var response = await httpClient.GetAsync($"api/place/details/json?placeid={Uri.EscapeUriString(placeId)}&key={_googleMapsKey}").ConfigureAwait(false);

                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        if (!string.IsNullOrWhiteSpace(json) && json != "ERROR")
                        {
                            result = new GooglePlace(JObject.Parse(json));
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
    }
}
