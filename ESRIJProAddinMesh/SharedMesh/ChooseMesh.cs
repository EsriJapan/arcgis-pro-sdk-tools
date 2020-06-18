using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Core.Portal;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ESRIJ.ArcGISPro
{
    public class ChooseMesh : ComboBox
    {
        #region 定数
        const string MeshLevel2 = "https://services.arcgis.com/P3ePLMYs2RVChkJx/arcgis/rest/services/JPN_Boundaries_ECM/FeatureServer/2";
        const string MeshLevel3 = "https://services.arcgis.com/P3ePLMYs2RVChkJx/arcgis/rest/services/JPN_Boundaries_ECM/FeatureServer/4";
        const string MeshLevel4 = "https://services.arcgis.com/P3ePLMYs2RVChkJx/arcgis/rest/services/JPN_Boundaries_ECM/FeatureServer/5";
        #endregion

        #region 起動時
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public ChooseMesh()
        {
            SharedMeshModule.chooseMesh = this;
            UpdateCombo();
        }
        #endregion

        #region イベント
        /// <summary>
        /// ArcGIS Online の地域メッシュを読み込む
        /// </summary>
        public void AddPortalLayer()
        {
            string url;

            if (this.Text == "2次メッシュ")
            {
                url = MeshLevel2;
            }
            else if (this.Text == "3次メッシュ")
            {
                url = MeshLevel3;
            }
            else if (this.Text == "4次メッシュ")
            {
                url = MeshLevel4;
            }
            else
            {
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("地域メッシュを選択してください。", "警告", 
                                                                 System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning, 
                                                                 System.Windows.MessageBoxResult.Yes);
                return;
            }

            try
            {
                QueuedTask.Run(() =>
                {
                    Layer lyr = LayerFactory.Instance.CreateLayer(new Uri(url), MapView.Active.Map);
                });
            }
            catch (System.ArgumentException )
            {
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("URLが無効です。ネットワーク接続を確認してください。", "エラー",
                                                                 System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error,
                                                                 System.Windows.MessageBoxResult.Yes);

                
            }
            catch (Exception )
            {
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("地域メッシュの共有に失敗しました。", "エラー",
                                                                 System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error,
                                                                 System.Windows.MessageBoxResult.Yes);

                
            }
        }

        /// <summary>
        /// コンボボックス初期化
        /// </summary>
        private void UpdateCombo()
        {
            Clear();

            Add(new ComboBoxItem(""));
            Add(new ComboBoxItem("2次メッシュ"));
            Add(new ComboBoxItem("3次メッシュ"));
            Add(new ComboBoxItem("4次メッシュ"));
        }

        /// <summary>
        /// コンボボックス選択イベント
        /// </summary>
        /// <param name="item">The newly selected combo box item</param>
        protected override void OnSelectionChange(ComboBoxItem item)
        {

            if (item == null)
                return;

            if (string.IsNullOrEmpty(item.Text))
                return;
  
        }
        #endregion
    }
}
