using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Portal;
using Esri.ArcGISRuntime.Security;
using Esri.ArcGISRuntime.Tasks.Offline;
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

namespace PlayGrounds
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {
        #region private variables
        private ArcGISPortal _portal = null;
        private Map _map = null;
        private string _dbFolder = @"D:\Temp\DemoData";
        private string _outGeodatabasePath = null;
        private string _tablename = null;
        private FeatureTable _table = null;
        private string _featureserviceUrl = "";
        #endregion
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void btnOpenWebmap_Click(object sender, RoutedEventArgs e)
        {
            Log("Creating portal");
            _portal = await ArcGISPortal.CreateAsync();

            Log("Setting credentials");
            _portal.Credential = await GetCredentials();

            string itemid = txtWebMapId.Text;

            Log("Creating portal item with id: " + itemid);
            PortalItem item = await Esri.ArcGISRuntime.Portal.PortalItem.CreateAsync(_portal, itemid);

            Log("Creating map");
            _map = new Map(item);

            Log("Adding map to mapview");
            MyMapView.Map = _map;

        }

        #region helper functions
        private void LogJobStatus(GenerateGeodatabaseJob job)
        {
            Log(string.Format("jobstatus: {0}", job.Status.ToString()));
        }

        private void Log(string msg)
        {
            Dispatcher.Invoke(new Action(() => { lstMessages.Items.Insert(0, String.Format("{0} - {1}", DateTime.Now.ToString("yyyyMMdd HH:mm:ss"), msg)); }));
            Console.WriteLine("{0} - {1}", DateTime.Now.ToString("yyyyMMdd HH:mm:ss"), msg);
        }

        private async Task<Credential> GetCredentials()
        {
            Credential cred = await AuthenticationManager.Current.GenerateCredentialAsync(
                                                new Uri("http://www.arcgis.com/sharing/rest"),
                                                "the_username",
                                                "the_P@ssw0rd") as ArcGISTokenCredential;
            //duplicate credential so the credentials are used for both www and services.arcgis.com
            Credential c2 = cred;
            c2.ServiceUri = new Uri("http://services.arcgis.com");

            AuthenticationManager.Current.AddCredential(c2);
            return cred;
        }

        #endregion

        private void btnEditting_Click(object sender, RoutedEventArgs e)
        {
            if (btnEditting.Content.ToString() == "Start Editing")
            {
                if (_map != null)
                {
                    FeatureLayer layer = _map.OperationalLayers.Where(t => t is FeatureLayer).FirstOrDefault() as FeatureLayer;
                    _table = layer.FeatureTable;
                    MyMapView.MouseDown += MyMapView_MouseDown;
                }
                btnEditting.Content = "Stop Editing";
            }
            else
            {
                btnEditting.Content = "Start Editing";
                MyMapView.MouseDown -= MyMapView_MouseDown;
            }
        }
        private async void MyMapView_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            MapPoint newLocation = MyMapView.ScreenToLocation(e.GetPosition(MyMapView));

            Feature newFeature = _table.CreateFeature();


            newFeature.Attributes["TYPE"] = "Speelplaats";

            if (e.RightButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                newFeature.Attributes["TYPE"] = "Trapveld";
            }

            newFeature.Geometry = newLocation;
            Log(String.Format("Creating new feature of type {0} at {1},{2}", newFeature.Attributes["TYPE"], newLocation.X, newLocation.Y));

            await _table.AddFeatureAsync(newFeature);

            if (_table is ServiceFeatureTable)
            {
                await ((ServiceFeatureTable)_table).ApplyEditsAsync();
            }
        }


        private async void btnCreateDB_Click(object sender, RoutedEventArgs e)
        {
            if (_map != null)
            {
                Log("Creating offline db");
                FeatureLayer layer = _map.OperationalLayers.Where(l => l is FeatureLayer).FirstOrDefault() as FeatureLayer;
                ServiceFeatureTable table = layer.FeatureTable as ServiceFeatureTable;

                _tablename = table.TableName;
                Log("Table name: " + _tablename);

                _featureserviceUrl = table.Source.ToString().Replace("/0", "");

                _outGeodatabasePath = System.IO.Path.Combine(_dbFolder, String.Format("TEMP_DB_{0}.geodatabase", DateTime.Now.ToString("yyyyMMdd_HHmmss")));

                Envelope extent = MyMapView.GetCurrentViewpoint(ViewpointType.BoundingGeometry).TargetGeometry as Envelope;

                Log("Creating Sync task");
                var gdbSyncTask = await GeodatabaseSyncTask.CreateAsync(new Uri(_featureserviceUrl), await GetCredentials());
                var generateGdbParams = await gdbSyncTask.CreateDefaultGenerateGeodatabaseParametersAsync(extent);
                generateGdbParams.SyncModel = SyncModel.Geodatabase;
                var generateGdbJob = gdbSyncTask.GenerateGeodatabase(generateGdbParams, _outGeodatabasePath);

                Log("Starting Sync job");
                generateGdbJob.Start();

                System.Timers.Timer t = new System.Timers.Timer(1000);
                t.Elapsed += async (s, Ee) =>
                {
                    bool result = await generateGdbJob.CheckStatusAsync();
                    Log(String.Format("Status check done: {0}: status: {1}", result, generateGdbJob.Status));
                    if (generateGdbJob.Status == Esri.ArcGISRuntime.Tasks.JobStatus.Succeeded || generateGdbJob.Status == Esri.ArcGISRuntime.Tasks.JobStatus.Failed)
                    {
                        if (generateGdbJob.Status == Esri.ArcGISRuntime.Tasks.JobStatus.Succeeded)
                        {
                                // if the job succeeded, add local data to the map
                                AddLocalLayerToMap();
                        }

                        t.Stop();
                        t.Close();
                    }
                };
                t.Start();

                Console.WriteLine("Submitted job #" + generateGdbJob.ServerJobId + " to create local geodatabase");

                _map.OperationalLayers.Remove(layer);
            }
        }

        private async void AddLocalLayerToMap()
        {
            Log("Adding layer to map");

            Geodatabase database = await Geodatabase.OpenAsync(_outGeodatabasePath);

            GeodatabaseFeatureTable table = database.GeodatabaseFeatureTable(_tablename);

            FeatureLayer layer = new FeatureLayer(table);
            _map.OperationalLayers.Add(layer);
            Log("Layer added from: " + _outGeodatabasePath);
        }


        private async void btnSync_Click(object sender, RoutedEventArgs e)
        {
            SyncGeodatabaseParameters taskParameters = new SyncGeodatabaseParameters()
            {
                RollbackOnFailure = true,
                GeodatabaseSyncDirection = SyncDirection.Bidirectional,
            };

            GeodatabaseSyncTask syncTask = await GeodatabaseSyncTask.CreateAsync(new Uri(_featureserviceUrl), await GetCredentials());

            Geodatabase database = await Esri.ArcGISRuntime.Data.Geodatabase.OpenAsync(_outGeodatabasePath);

            SyncGeodatabaseJob job = syncTask.SyncGeodatabase(taskParameters, database);

            job.JobChanged += (s, je) =>
            {
                if (job.Status == Esri.ArcGISRuntime.Tasks.JobStatus.Succeeded)
                {
                    Log("Synchronization is complete!");
                }
                else if (job.Status == Esri.ArcGISRuntime.Tasks.JobStatus.Failed)
                {
                    Log("Error: " + job.Error.Message);
                }
                else
                {
                    Log(string.Format("Sync in progress ... status: {0}", job.Status));
                }
            };


            var result = await job.GetResultAsync();
        }

    }
}