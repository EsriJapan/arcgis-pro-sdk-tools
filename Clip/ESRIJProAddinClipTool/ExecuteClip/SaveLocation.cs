using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArcGIS.Desktop.Catalog;
using ArcGIS.Desktop.Framework.Contracts;

namespace ESRIJ.ArcGISPro
{
    public class SaveLocation : EditBox
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SaveLocation()
        {
            ClipModule.SaveLocation = this;
        }

        /// <summary>
        /// ダイアログ起動
        /// </summary>
        public void ShowDialog()
        {
            OpenItemDialog searchItemDialog = new OpenItemDialog();

            searchItemDialog.Title = "ファイルジオデータベースを選択";
            searchItemDialog.MultiSelect = false;
            searchItemDialog.Filter = ItemFilters.geodatabases;

            var ok = searchItemDialog.ShowDialog();
            if (ok != true)
                return;

            var selectedItems = searchItemDialog.Items;
            foreach (var selectedItem in selectedItems)
                Text = selectedItem.Path;
        }
    }
}
