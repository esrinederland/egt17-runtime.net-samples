
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MarsLandings.ViewModels
{
    public class MapViewModel : NotificationBase
    {

        #region Fields & Constants
        
        // Constants to the used URI
        private const string CURIOSITYURL = "http://services.arcgis.com/emS4w7iyWEQiulAb/arcgis/rest/services/TRACK_CURIOSITY/FeatureServer/0";
        private const string LANDINGSITESURL = "http://services.arcgis.com/emS4w7iyWEQiulAb/arcgis/rest/services/Mars_Landing_Sites/FeatureServer/0";
        private const string BASEMAPURL = "https://webgis2.wr.usgs.gov/arcgis/rest/services/Mars_color/MapServer";

        // Fields
        ObservableCollection<LandingSite> _LandingSites;
        LandingSite _LandingSite;
        Scene _Scene;
        Viewpoint _ViewpointRequested;
        bool _LandingSitesEnabled;

        #endregion

        /// <summary>
        /// Default constructor
        /// </summary>
        public MapViewModel()
        {
            // create a scene
            CreateMap();

            // Fill ComboBox with landing sites
            SetLandingSiteCollection();
        }

        #region Properties

        /// <summary>
        /// XAML bindable implementation of LandingSites.
        /// </summary>
        public ObservableCollection<LandingSite> LandingSites
        {
            get { return _LandingSites; }
            set
            {
                SetProperty(ref _LandingSites, value);
                LandingSitesEnabled = _LandingSites.Count() > 0;
            }
        }

        /// <summary>
        /// Property for the combobox to check if there are landing sites present
        /// </summary>
        public bool LandingSitesEnabled
        {
            get { return _LandingSitesEnabled; }
            set { SetProperty(ref _LandingSitesEnabled, value); }
        }

        /// <summary>
        /// XAML bindable implementation of the selected LandingSite.
        /// </summary>
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

        /// <summary>
        /// XAML bindable implementation of Scene.
        /// </summary>
        public Scene Scene
        {
            get { return _Scene; }
            set { SetProperty(ref _Scene, value); }
        }

        /// <summary>
        /// property to zoom the the slected landingsite location
        /// </summary>
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
            
            // add basemap layer
            ArcGISMapImageLayer imageLayer = new ArcGISMapImageLayer(new Uri(BASEMAPURL));
            scene.Basemap.BaseLayers.Add(imageLayer);

            // create feature layers for the landingsites and curiosity path
            FeatureLayer landingSites = new FeatureLayer(new Uri(LANDINGSITESURL));
            FeatureLayer curiosity = new FeatureLayer(new Uri(CURIOSITYURL));

            // add the feature layers to the scene
            scene.OperationalLayers.Add(curiosity);
            scene.OperationalLayers.Add(landingSites);

            // assign the new created scene to the bindable implementation of Scene
            Scene = scene;
        }

        /// <summary>
        /// Create's an ObservableCollection of LandingSites used to assing to the bindable implementation of LandingSites
        /// </summary>
        private async void SetLandingSiteCollection()
        {
            // empty, default parameters
            QueryParameters parameters = new QueryParameters()
            {
                WhereClause = "1=1",
                ReturnGeometry = true
            };

            string[] outputFields = new string[] { "*" };

            // ServiceFeatureTable creation from the Landingsite uri
            ServiceFeatureTable table = new ServiceFeatureTable(new Uri(LANDINGSITESURL))
            {
                FeatureRequestMode = FeatureRequestMode.ManualCache
            };

            // request /retrieve all the availble landigsites
            FeatureQueryResult featureResult = await table.PopulateFromServiceAsync(parameters, true, outputFields);

            // place the retrieved features in a list of features
            List<Feature> features = featureResult.ToList();

            // creation of a list of LandingSite objects 
            List<LandingSite> landingSites = new List<LandingSite>();

            // create a LandingSite object with the name and location and add them to the list
            features.ForEach(f => landingSites.Add(new LandingSite()
            {
                Name = f.Attributes["FULL_NAME"].ToString(),
                Location = f.Geometry as MapPoint
            }));

            // assign the new created list of lnadingsite objects to the bindable implementation of LandingSites
            LandingSites = new ObservableCollection<LandingSite>(landingSites);
        }
    }

    /// <summary>
    /// Simple LandingSite object with a name and location mappoint
    /// </summary>
    public class LandingSite    
    {
        public string Name { get; set; }

        public MapPoint Location { get; set; }
    }
}
