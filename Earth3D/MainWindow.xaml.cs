using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Earth3D
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private GraphicsOverlay _positionGraphicsOverlay = null;
        private Graphic _currentPosition = null;
        public MainWindow()
        {
            InitializeComponent();
            Init();
        }

        private void Init()
        {
            //set the suntime so that the earth shows the sun as it is now
            MySceneView.SunTime = new DateTimeOffset(DateTime.Now);
            Scene scene = new Scene(BasemapType.Topographic);

            MySceneView.Scene = scene;

            SimpleMarkerSymbol symbol = new SimpleMarkerSymbol() { Style = SimpleMarkerSymbolStyle.Circle, Color = Colors.Orange, Size = 15 };
            Renderer renderer = new SimpleRenderer(symbol);
            _positionGraphicsOverlay = new GraphicsOverlay();
            _positionGraphicsOverlay.Renderer = renderer;
            _positionGraphicsOverlay.SceneProperties.SurfacePlacement = SurfacePlacement.Relative;

            MySceneView.GraphicsOverlays.Add(_positionGraphicsOverlay);

            System.Timers.Timer t = new System.Timers.Timer(1000);
            t.Elapsed += async (s, Ee) =>
            {
                try
                {
                    await GetPosition();
                }
                catch (Exception ex)
                {
                    //TODO: error handling
                }
            };
            t.Start();

        }
        private async Task GetPosition()
        {
            //download json
            System.Net.WebClient wc = new System.Net.WebClient();
            string response = wc.DownloadString("http://api.open-notify.org/iss-now.json");

            ////parse JSON and get lat and lon
            dynamic issData = JObject.Parse(response);

            double x = issData.iss_position.longitude;
            double y = issData.iss_position.latitude;

            //create new mappoint with height of 400km (that is the height of the ISS)
            var location = new MapPoint(x, y, 400 * 1000, SpatialReferences.Wgs84);

            //create if position if it does not exist
            if (_currentPosition == null)
            {
                Uri u = new Uri("Icons/Station01/model.dae", UriKind.Relative);
                ModelSceneSymbol issSymbol = await ModelSceneSymbol.CreateAsync(u, 1000);
                issSymbol.Pitch = 90;
                issSymbol.Roll = 90;
                _currentPosition = new Graphic(location, issSymbol);
                _positionGraphicsOverlay.Graphics.Add(_currentPosition);
            }
            else
            {
                //add a new graphic on the last position so a trail appears
                Graphic previousPosition = new Graphic(_currentPosition.Geometry);

                _positionGraphicsOverlay.Graphics.Add(previousPosition);
            }

            //set the location every update  
            _currentPosition.Geometry = location;

            UpdateText();
        }
        private void btnCurrent_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPosition != null)
            {
                MapPoint lastlocation = _currentPosition.Geometry as MapPoint;

                //Zoom to 100 km from the last location with an angle of 35
                Camera newCamera = new Camera(lastlocation, 100 * 1000, 0, 35, 0);
                MySceneView.SetViewpointCameraAsync(newCamera);
            }
        }

        private void UpdateText()
        {
            Dispatcher.Invoke(new Action(() => { txtNrofPos.Text = String.Format("Aantal posities: {0}", _positionGraphicsOverlay.Graphics.Count); }));
        }

    }
}
