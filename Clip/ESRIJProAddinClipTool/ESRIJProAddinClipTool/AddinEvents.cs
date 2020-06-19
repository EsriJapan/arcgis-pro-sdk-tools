using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ESRIJ.ArcGISPro
{
    /// <summary>
    /// クリップ格納先設定
    /// </summary>
    internal class EJClip_DialogButton : Button
    {
        protected override void OnClick()
        {
            ClipModule.SaveLocation.ShowDialog();
        }
    }

    /// <summary>
    /// クリップ範囲設定
    /// </summary>
    internal class EJClip_Drawing : Button
    {
        protected override void OnClick()
        {
            var buttonID = this.ID;

            if (buttonID == "EJClip_RectanbleButton")
            {
                ClipModule.MapToolID = "EJClip_RectanbleMapTool";
            }
            else if (buttonID == "EJClip_PolygonButton")
            {
                ClipModule.MapToolID = "EJClip_PolygonMapTool";
            }
            else
            {
                ClipModule.MapToolID = "EJClip_SelectPolygonMapTool";
            }
            
            ClipModule.clipEvents.ActivateDrawingTool();
        }
    }

    /// <summary>
    /// クリップ実行
    /// </summary>
    internal class EJClip_ClipButton : Button
    {
        protected override void OnClick()
        {
            ClipModule.clipManager.RunClip();
        }
    }

    /// <summary>
    /// クリップクリアー
    /// </summary>
    internal class EJClip_ClearButton : Button
    {
        protected override void OnClick()
        {
            ClipModule.clipManager.RunClear();        
        }
    }

}
