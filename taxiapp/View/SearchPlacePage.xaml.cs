using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using taxiapp.ViewModel;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace taxiapp.View
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SearchPlacePage : ContentPage
    {
        MainPageViewModel vm;
        public SearchPlacePage(MainPageViewModel MainVM)
        {
            InitializeComponent();
            vm = new MainPageViewModel();
            vm = MainVM;
            vm.DestEntry = DestinationEntry;
        }
        protected override void OnAppearing()
        {
            base.OnAppearing();
            OriginEntry.Focus();
        }
    }
}