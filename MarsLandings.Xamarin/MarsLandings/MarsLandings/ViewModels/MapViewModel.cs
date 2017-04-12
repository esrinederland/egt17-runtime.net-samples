using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MarsLandings.ViewModels
{
    public class MapViewModel : NotificationBase
    {

        #region Fields & Constants
        
        // Constants
        private const string CURIOSITYURL = "http://services.arcgis.com/emS4w7iyWEQiulAb/arcgis/rest/services/TRACK_CURIOSITY/FeatureServer/0";
        private const string LANDINGSITESURL = "http://services.arcgis.com/emS4w7iyWEQiulAb/arcgis/rest/services/Mars_Landing_Sites/FeatureServer/0";
        private const string BASEMAPURL = "https://webgis2.wr.usgs.gov/arcgis/rest/services/Mars_color/MapServer";

        // Fields
        ObservableCollection<LandingSite> _LandingSites;
        LandingSite _LandingSite;
        Scene _Scene;
        GraphicsOverlay _Overlays;
        Viewpoint _ViewpointRequested;
        bool _LandingSitesEnabled;

        #endregion

        public MapViewModel()
        {

            Overlays = new GraphicsOverlay();

            CreateMap();


            // Fill ComboBox with landing sites
            SetLandingSiteCollection();
        }

        #region Properties

        public ObservableCollection<LandingSite> LandingSites
        {
            get { return _LandingSites; }
            set
            {
                SetProperty(ref _LandingSites, value);
                LandingSitesEnabled = _LandingSites.Count() > 0;
            }
        }

        public bool LandingSitesEnabled
        {
            get { return _LandingSitesEnabled; }
            set { SetProperty(ref _LandingSitesEnabled, value); }
        }

        public LandingSite SelectedLandingSite
        {
            get
            {
                return _LandingSite;
            }
            set
            {
                if (_LandingSite != value)
                {
                    SetProperty(ref _LandingSite, value);
                    ViewpointRequested = new Viewpoint(_LandingSite.Location, new Camera(_LandingSite.Location, 400000, 0, 60, 0));
                }
            }
        }

        public GraphicsOverlay Overlays
        {
            get { return _Overlays; }
            set { SetProperty(ref _Overlays, value); }
        }

        /// <summary>
        /// XAML bindable implementation of Scene.
        /// </summary>
        public Scene Scene
        {
            get { return _Scene; }
            set { SetProperty(ref _Scene, value); }
        }

        public Viewpoint ViewpointRequested
        {
            get { return _ViewpointRequested; }
            private set { SetProperty(ref _ViewpointRequested, value); }
        }

        #endregion

        /// <summary>
        /// Create Map and add a FeatureLayer to the map
        /// </summary>
        private void CreateMap()
        {
            Scene scene = new Scene();
            
            ArcGISMapImageLayer imageLayer = new ArcGISMapImageLayer(new Uri(BASEMAPURL));
            scene.Basemap.BaseLayers.Add(imageLayer);

            FeatureLayer landingSites = new FeatureLayer(new Uri(LANDINGSITESURL));

            FeatureLayer curiosity = new FeatureLayer(new Uri(CURIOSITYURL));

            scene.OperationalLayers.Add(curiosity);

            scene.OperationalLayers.Add(landingSites);

            Scene = scene;
        }

        private async void SetLandingSiteCollection()
        {
            QueryParameters parameters = new QueryParameters()
            {
                WhereClause = "1=1",
                ReturnGeometry = true
            };

            var outputFields = new string[] { "*" };

            var table = new ServiceFeatureTable(new Uri(LANDINGSITESURL))
            {
                FeatureRequestMode = FeatureRequestMode.ManualCache
            };

            FeatureQueryResult featureResult = await table.PopulateFromServiceAsync(parameters, true, outputFields);

            List<Feature> features = featureResult.ToList();

            List<LandingSite> landingSites = new List<LandingSite>();

            features.ForEach(f => landingSites.Add(new LandingSite() { Name = f.Attributes["FULL_NAME"].ToString(), Location = f.Geometry as MapPoint }));

            LandingSites = new ObservableCollection<LandingSite>(landingSites);
        }
    }

    public class LandingSite
    {
        public string Name { get; set; }

        public MapPoint Location { get; set; }
    }
}
