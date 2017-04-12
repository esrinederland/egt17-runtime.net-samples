using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI.Controls;
using System;
#if NETFX_CORE
using Windows.UI.Xaml;
#else
using System.Windows;
#endif

namespace MarsLandings.Controllers
{
    /// <summary>
    /// Binding helpers
    /// </summary>
    public class CommandBinder
    {
        /// <summary>
        /// This command binding allows you to set the extent on a mapView from your view-model through binding
        /// </summary>
        public static Envelope GetRequestView(DependencyObject obj)
        {
            return (Envelope)obj.GetValue(RequestViewProperty);
        }

        /// <summary>
        /// This command binding allows you to set the extent on a mapView from your view-model through binding
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="extent"></param>
        public static void SetRequestView(DependencyObject obj, Envelope extent)
        {
            obj.SetValue(RequestViewProperty, extent);
        }

        /// <summary>
        /// Identifies the ZoomTo Attached Property.
        /// </summary>
        public static readonly DependencyProperty RequestViewProperty =
            DependencyProperty.RegisterAttached("RequestView", typeof(Viewpoint), typeof(CommandBinder), new PropertyMetadata(null, RequestViewPropertyChanged));

        
        /// <summary>
        /// Method to set a location, to which the scene has to zoom to 
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private async static void RequestViewPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SceneView)
            {
                SceneView mapView = d as SceneView;
                if (e.NewValue is Viewpoint)
                {
                    await mapView.SetViewpointAsync((Viewpoint)e.NewValue, new TimeSpan(0,0,0,2));
                }
            }
        }
    }
}
