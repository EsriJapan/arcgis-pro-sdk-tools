using ArcGIS.Core.Events;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Core.Geoprocessing;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Events;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;


namespace ESRIJ.ArcGISPro
{
    public static class ClipModule
    {
        public static SaveLocation SaveLocation { get; set; }

        public static string MapToolID { get; set; }

        public static Geometry PolygonForClip { get; set; }

        public static FeatureLayer PolygonLayerForClip { get; set; }

        public static System.IDisposable OverlayObject = null;
        public static ClipEvents clipEvents;
        public static ClipManager clipManager;

        public static void Initialize()
        {
            clipEvents = new ClipEvents();
            clipManager = new ClipManager();
        }

        /// <summary>
        /// グラフィック削除機能
        /// </summary>
        public static void RemoveFromMapOverlay()
        {
            if (OverlayObject != null)
            {
                OverlayObject.Dispose();
                OverlayObject = null;
                
            }
        }

        /// <summary>
        /// クリップ用のポリゴン削除機能
        /// </summary>
        public static void RemovePolygonForClip()
        {
            if (PolygonForClip != null)
            {
                PolygonForClip = null;
                PolygonLayerForClip = null;
            } 
        }

        /// <summary>
        /// 選択解除機能
        /// </summary>
        public static void ClearSelection()
        {
            var cmd = FrameworkApplication.GetPlugInWrapper("esri_mapping_clearSelectionButton") as ICommand;
            if (cmd.CanExecute(null))
                cmd.Execute(null);
        }

        /// <summary>
        /// レイヤー一覧取得用メソッド
        /// </summary>
        public static IEnumerable<BasicFeatureLayer> GetLayers()
        {
            var gdbPath = ClipModule.SaveLocation.Text;

            if (ClipModule.PolygonLayerForClip == null)
            {
                return MapView.Active.Map.GetLayersAsFlattenedList().OfType<BasicFeatureLayer>().Where(f => f.IsVisible == true && f.IsSelectable == true);


            }
            else
            {
                return MapView.Active.Map.GetLayersAsFlattenedList().
                           OfType<BasicFeatureLayer>().Where(f => f.Name != ClipModule.PolygonLayerForClip.Name &&
                                                            f.IsVisible == true && f.IsSelectable == true);
            }
        }

        /// <summary>
        /// ジオプロ実行用メソッド
        /// </summary>
        public static Task<IGPResult> ExecuteGeoprocessingTool(string tool, IReadOnlyList<string> parameters)
        {
            return Geoprocessing.ExecuteToolAsync(tool, parameters);
        }

    }
}
