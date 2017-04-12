using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Xamarin.Forms;
using System;
using Xamarin.Forms;

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
        public static Envelope GetRequestView(BindableObject obj)
        {
            return (Envelope)obj.GetValue(RequestViewProperty);
        }

        /// <summary>
        /// This command binding allows you to set the extent on a mapView from your view-model through binding
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="extent"></param>
        public static void SetRequestView(BindableObject obj, Envelope extent)
        {
            obj.SetValue(RequestViewProperty, extent);
        }

        /// <summary>
        /// Identifies the ZoomTo Attached Property.
        /// </summary>
        public static readonly BindableProperty RequestViewProperty =
            BindableProperty.CreateAttached("RequestView", typeof(Viewpoint), typeof(CommandBinder), null, propertyChanged: RequestViewPropertyChanged);
        

        //public static readonly DependencyProperty 

        private async static void RequestViewPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            bool canCast = TryCast(bindable, out SceneView view);

            if (!canCast) return;

            if (newValue is Viewpoint)
            {
                await view.SetViewpointAsync((Viewpoint)newValue, new TimeSpan(0, 0, 0, 2));
            }
        }

        private static bool TryCast<T>(object obj, out T result)
        {
            if (obj is T)
            {
                result = (T)obj;
                return true;
            }

            result = default(T);
            return false;
        }
    }
}
