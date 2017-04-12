using MarsLandings.ViewModels;
using Xamarin.Forms;

namespace MarsLandings
{
    public partial class MainPage : ContentPage
	{
        public MapViewModel VM { get; } = new MapViewModel();

        public MainPage()
        {
            InitializeComponent();

            BindingContext = VM;
        }
    }
}
