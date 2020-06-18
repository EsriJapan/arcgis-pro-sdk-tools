using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Core.Geoprocessing;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;

namespace ESRIJ.ArcGISPro
{
    public class ClipArea : MapTool
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public ClipArea()
        {
            IsSketchTool = true;
            
            if (ClipModule.MapToolID == "EJClip_RectanbleMapTool")
            {
                SketchType = SketchGeometryType.Rectangle;
            }
            else if (ClipModule.MapToolID == "EJClip_PolygonMapTool")
            {
                SketchType = SketchGeometryType.Polygon;
            }
            else if (ClipModule.MapToolID == "EJClip_SelectPolygonMapTool")
            {
                SketchType = SketchGeometryType.Point;
            }
            
            SketchOutputMode = SketchOutputMode.Map;
        }

        /// <summary>
        /// マップツールアクティブイベント
        /// </summary>
        protected override Task OnToolActivateAsync(bool active)
        {
            return base.OnToolActivateAsync(active);
        }

        /// <summary>
        /// マップツールでの描画完了イベント
        /// </summary>
        protected override Task<bool> OnSketchCompleteAsync(Geometry geometry)
        {
            return QueuedTask.Run(() =>
            {
                if (ClipModule.MapToolID != "EJClip_SelectPolygonMapTool")
                {
                    ClipModule.PolygonForClip = geometry;
                }

                MapView.Active.SelectFeatures(geometry);
                return true;
            });
        }
    }
}
