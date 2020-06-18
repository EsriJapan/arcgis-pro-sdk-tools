using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ESRIJ.ArcGISPro
{
    /// <summary>
    /// 地域メッシュ作成
    /// </summary>
    internal class EJMesh_ShowButton : Button
    {

        protected override void OnClick()
        {
            DockPane pane = FrameworkApplication.DockPaneManager.Find("EJMesh_DockPane");
            if (pane == null)
                return;

            pane.Activate();
        }
    }

    /// <summary>
    /// 地域メッシュ共有
    /// </summary>
    internal class ShareMesh : Button
    {
        protected override void OnClick()
        {
            QueuedTask.Run(() =>
            {
                SharedMeshModule.chooseMesh.AddPortalLayer();
            });
        }
    }
}
