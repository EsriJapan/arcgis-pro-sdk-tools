using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Catalog;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Core.Geoprocessing;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;

namespace ESRIJ.ArcGISPro
{
    public　class MeshCreator: BindBase
    {
        #region プロパティ
        public bool RadioNew { get; set; }
        public bool RadioAdd { get; set; }
        public string GdbPath { get; set; }
        public string FeatureClassPath { get; set; }
        public List<String> Fields { get; set; }
        public string SelectedField { get; set; }
        public EJMeshCalculator EJMeshCalculator { get; set; }
        #endregion

        #region 地域メッシュ作成
        /// <summary>
        /// 地域メッシュ作成メソッド
        /// </summary>
        public void MakeMesh()
        {
            var mapView = MapView.Active;
            if (mapView == null)
            {
                MessageBox.Show("マップをアクティブにしてください", "警告",
                                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning,
                                System.Windows.MessageBoxResult.Yes);
                return;
            }

            // 入力チェック
            if (CheckInput() == false)
            {
                MessageBox.Show("地域メッシュの計算を行ってください", "警告",
                                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning,
                                System.Windows.MessageBoxResult.Yes);
                return;
            }

            var result = MessageBox.Show("地域メッシュを作成しますか？", "確認",
                                         System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Information,
                                         System.Windows.MessageBoxResult.Yes);

            if (result == System.Windows.MessageBoxResult.No)
            {
                return;
            }
            else
            {
                // 既存のフィーチャクラスに追加する場合
                if (RadioAdd == true)
                {
                    if (FeatureClassPath == null)
                    {
                        MessageBox.Show("作成先を指定してください。", "警告",
                                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning,
                                        System.Windows.MessageBoxResult.Yes);
                        return;
                    }

                    if (SelectedField == null)
                    {
                        MessageBox.Show("フィールドを指定してください。", "警告",
                                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning,
                                        System.Windows.MessageBoxResult.Yes);
                        return;
                    }

                    // 既存のフィーチャクラスに地域メッシュを作成
                    AddFeature();
                }
                // 新規フィーチャクラスを作成する場合
                else
                {
                    if (GdbPath == null)
                    {
                        MessageBox.Show("出力先を指定してください。", "警告",
                                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning,
                                        System.Windows.MessageBoxResult.Yes);
                        return;
                    }

                    // 新規フィーチャークラスに地域メッシュを作成
                    ExportFeature();
                }
            }
        }

        /// <summary>
        /// 入力値チェック
        /// </summary>
        private bool CheckInput()
        {
            // 計算せずに地域メッシュを作成しようとした場合
            if (EJMeshCalculator.HighlightPolygon == null)
            {
                return false;
            }

            // ラジオボタンを選択しているけど、地域コードが空の場合（手で削除したり）
            if (EJMeshCalculator.RadioFirstMesh == true)
            {
                if (EJMeshCalculator.FirstMesh == "" || EJMeshCalculator.FirstMesh == null)
                {
                    return false;
                }
            }
            
            if (EJMeshCalculator.RadioSecondMesh == true)
            {
                if (EJMeshCalculator.SecondMesh == "" || EJMeshCalculator.SecondMesh == null)
                {
                    return false;
                }
            }
            
            if (EJMeshCalculator.RadioThirdMesh == true)
            {
                if (EJMeshCalculator.ThirdMesh == "" || EJMeshCalculator.ThirdMesh == null)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// フィーチャークラス作成
        /// </summary>
        private async Task<bool> MakeFeatureClass(string feacureClassName)
        {
            try
            {
                var mapView = MapView.Active;

                // フィーチャークラス作成
                IGPResult resultFeatureClass = await ExecuteGeoprocessingTool("CreateFeatureclass_management", 
                                                                              Geoprocessing.MakeValueArray(GdbPath,
                                                                                                           feacureClassName,
                                                                                                           "POLYGON",
                                                                                                           "",
                                                                                                           "DISABLED",
                                                                                                           "DISABLED",
                                                                                                           mapView.Map.SpatialReference));

                if (resultFeatureClass.IsFailed)
                {
                    return false;
                }

                FeatureLayer featureLayer = mapView.Map.FindLayers(feacureClassName).FirstOrDefault() as FeatureLayer;

                if (featureLayer == null)
                {
                    // SDEの場合、フィーチャークラス名に「CHIBA_SDE.DBO」がついてCHIBA_SDE.DBO.地域メッシュ_52396526のようになってしまうのでそれの対応
                    var pos = resultFeatureClass.Values.FirstOrDefault().LastIndexOf("\\");
                    var featureLayerName = resultFeatureClass.Values.FirstOrDefault().Substring(pos + 1, resultFeatureClass.Values.FirstOrDefault().Length - pos - 1);
                    featureLayer = mapView.Map.FindLayers(featureLayerName).FirstOrDefault() as FeatureLayer;
                }
                        
                // カラム追加
                IGPResult iGPField = await ExecuteGeoprocessingTool("AddField_management", 
                                                                    Geoprocessing.MakeValueArray(featureLayer,
                                                                                                 "MESH_CODE",
                                                                                                 "TEXT"));
                if (iGPField.IsFailed)
                {
                    return false;
                }

                return true;
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// ジオプロ実行用メソッド
        /// </summary>
        private async Task<IGPResult> ExecuteGeoprocessingTool(string tool, IReadOnlyList<string> parameters)
        {
            return await Geoprocessing.ExecuteToolAsync(tool, parameters);
        }

        /// <summary>
        /// 新規にフィーチャークラス作成してそこに地域メッシュを登録
        /// </summary>
        private async void ExportFeature()
        {
            var editOperation = new EditOperation();
            var pd = new ProgressDialog("実行中です");
            pd.Show();

            try
            {
                // フィーチャークラス名（地域メッシュ_地域コード）作成
                var feacureClassName = "地域メッシュ_" + EJMeshCalculator.HighlightPolygon.FirstOrDefault().Key;

                // フィーチャクラスの存在チェック
                var check = await QueuedTask.Run(() =>
                {
                    return FeatureClassExists(GdbPath, feacureClassName);
                });

                if (check == true)
                {
                    pd.Hide();

                    MessageBox.Show(feacureClassName + "はすでに" + GdbPath + "に存在しています。", "地域メッシュ作成",
                                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning,
                                    System.Windows.MessageBoxResult.Yes);

                    return;
                }

                // フィーチャークラス作成
                bool result = await MakeFeatureClass(feacureClassName);

                if (result == false)
                    throw new Exception();

                await QueuedTask.Run(() =>
                {
                    using (Geodatabase gdb = GetGdb(GdbPath))
                    {
                        using (FeatureClass featureClass = gdb.OpenDataset<FeatureClass>(Path.GetFileName(feacureClassName)))
                        {
                            // 作成したフィーチャークラスに地域メッシュを登録
                            Polygon polygon = PolygonBuilder.CreatePolygon(EJMeshCalculator.HighlightPolygon.FirstOrDefault().Value);

                            string shapeField = featureClass.GetDefinition().GetShapeField();

                            var attributes = new Dictionary<string, object>();
                            attributes.Add(shapeField, polygon);
                            attributes.Add("MESH_CODE", EJMeshCalculator.HighlightPolygon.FirstOrDefault().Key);

                            editOperation.Create(featureClass, attributes);
                            editOperation.Execute();
                        }
                    }        
                });

                if (editOperation.IsSucceeded == false)
                {
                    throw new Exception();
                }

                await Project.Current.SaveEditsAsync();
            }
            catch (Exception )
            {
                await editOperation.UndoAsync();

                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("新規フィーチャークラスへの地域メッシュの登録に失敗しました。", "エラー",
                                                                 System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error,
                                                                 System.Windows.MessageBoxResult.Yes);

                
            }
            finally
            {
                pd.Dispose();
            }
        }

        /// <summary>
        /// GDB取得用メソッド
        /// </summary>
        private Geodatabase GetGdb(string path)
        {
            if (RadioAdd)
            {
                if (FeatureClassPath != null)
                {
                    if (FeatureClassPath.Contains(".sde"))
                    {

                        return new Geodatabase(new DatabaseConnectionFile(new Uri(Path.GetDirectoryName(path))));
                    }
                    else
                    {
                        return new Geodatabase(new FileGeodatabaseConnectionPath(new Uri(Path.GetDirectoryName(path))));
                    }
                }
            }
            else
            {
                if (GdbPath != null)
                {
                    if (GdbPath.Contains(".sde"))
                    {

                        return new Geodatabase(new DatabaseConnectionFile(new Uri(path)));
                    }
                    else
                    {
                        return new Geodatabase(new FileGeodatabaseConnectionPath(new Uri(path)));
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 既存のフィーチャクラスに対する地域メッシュ登録処理
        /// </summary>
        private async void AddFeature()
        {
            var mapView = MapView.Active;

            var editOperation = new EditOperation();

            try
            {
                // レイヤーの属性テーブルにアクセスして、フィールド名を Fields 配列に格納する　
                await QueuedTask.Run(() =>
                {
                    using(Geodatabase gdb = GetGdb(FeatureClassPath))
                    {
                        using (FeatureClass featureClass = gdb.OpenDataset<FeatureClass>(Path.GetFileName(FeatureClassPath)))
                        {
                            var feacureClassName = Path.GetFileName(FeatureClassPath);

                            FeatureLayer featureLayer = null;
                            featureLayer = mapView.Map.FindLayers(feacureClassName).FirstOrDefault() as FeatureLayer;

                            // 指定したフィーチャークラスをマップに表示させる
                            if (featureLayer == null)
                            {
                                using (Table table = gdb.OpenDataset<Table>(Path.GetFileName(FeatureClassPath)))
                                {
                                    var alias = table.GetDefinition().GetAliasName();

                                    // エイリアスも確認
                                    if (alias != feacureClassName)
                                    {
                                        featureLayer = mapView.Map.FindLayers(alias).FirstOrDefault() as FeatureLayer;

                                        if (featureLayer == null)
                                        {
                                            featureLayer = LayerFactory.Instance.CreateFeatureLayer(featureClass, MapView.Active.Map);
                                        }
                                    }
                                    else
                                    {
                                        featureLayer = LayerFactory.Instance.CreateFeatureLayer(featureClass, MapView.Active.Map);
                                    }
                                }
                            }

                            // 指定したフィーチャークラスに地域メッシュを登録
                            Polygon polygon = PolygonBuilder.CreatePolygon(EJMeshCalculator.HighlightPolygon.FirstOrDefault().Value);
                            string shapeField = featureClass.GetDefinition().GetShapeField();

                            var attributes = new Dictionary<string, object>();
                            attributes.Add(shapeField, polygon);
                            attributes.Add(SelectedField, EJMeshCalculator.HighlightPolygon.FirstOrDefault().Key);

                            editOperation.Create(featureLayer, attributes);
                            editOperation.Execute();
   
                        }
                    }
                });

                await Project.Current.SaveEditsAsync();
            }
            catch (Exception )
            {
                await editOperation.UndoAsync();

                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("既存フィーチャークラスへの地域メッシュの登録に失敗しました。", "エラー",
                                                                 System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error,
                                                                 System.Windows.MessageBoxResult.Yes);

                
            }
        }
        #endregion

        #region その他関連処理
        /// <summary>
        /// コンボボックスで選択したレイヤーのフィールド名のリストを取得
        /// </summary>
        private void GetFields()
        {
            try
            {
                // レイヤーの属性テーブルにアクセスして、フィールド名を Fields 配列に格納する　
                QueuedTask.Run(() =>
                {                 
                    Geodatabase gdb = GetGdb(FeatureClassPath);

                    using (FeatureClass featureClass = gdb.OpenDataset<FeatureClass>(Path.GetFileName(FeatureClassPath)))
                    {
                        var flf = featureClass.GetDefinition().GetFields();

                        // 文字列型のフィールドを抽出する
                        Fields = flf.Where(f => f.FieldType == FieldType.String).Select(f => f.Name).ToList();
                        OnPropertyChanged(nameof(Fields));
                    }                    
                });
            }
            catch (Exception )
            {
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("フィールドの取得に失敗しました。", "エラー",
                                                                 System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error,
                                                                 System.Windows.MessageBoxResult.Yes);

                
            }
        }

        /// <summary>
        /// フィーチャークラス存在チェック
        /// </summary>
        private bool FeatureClassExists(string geodatabase, string featureClassName)
        {
            try
            {
                var fileGDBpath = new FileGeodatabaseConnectionPath(new Uri(geodatabase));

                using (Geodatabase gdb = new Geodatabase(fileGDBpath))
                {
                    FeatureClassDefinition featureClassDefinition = gdb.GetDefinition<FeatureClassDefinition>(featureClassName);
                    featureClassDefinition.Dispose();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// FGDBもしくはフィーチャークラスを選択するダイアログ起動
        /// </summary>
        public void ShowDialog()
        {
            OpenItemDialog searchItemDialog = new OpenItemDialog();

            if (RadioAdd == true)
            {
                BrowseProjectFilter bf = new BrowseProjectFilter
                {
                    Name = "ポリゴンフィーチャークラス"
                };

                // プロジェクトの中の以下のみを参照
                bf.Includes.Add("FolderConnection");
                bf.Includes.Add("GDB");

                // 上記アイテム内の以下のみを参照 
                bf.AddDoBrowseIntoTypeId("database_fgdb");
                bf.AddDoBrowseIntoTypeId("database_egdb");

                // 上記アイテムの以下のみを参照
                bf.AddCanBeTypeId("fgdb_fc_polygon");
                bf.AddCanBeTypeId("egdb_fc_polygon");

                searchItemDialog.Title = "ポリゴンフィーチャクラスを選択";
                searchItemDialog.MultiSelect = false;
                searchItemDialog.BrowseFilter = bf;

                var ok = searchItemDialog.ShowDialog();
                if (ok != true)
                    return;

                var selectedItems = searchItemDialog.Items;
                foreach (var selectedItem in selectedItems)
                    FeatureClassPath = selectedItem.Path;

                OnPropertyChanged(nameof(FeatureClassPath));

                // フィーチャークラスのフィールドを取得
                GetFields();
            }
            else
            {
                searchItemDialog.Title = "ファイルジオデータベースを選択";
                searchItemDialog.MultiSelect = false;
                searchItemDialog.Filter = ItemFilters.geodatabases;

                var ok = searchItemDialog.ShowDialog();
                if (ok != true)
                    return;

                var selectedItems = searchItemDialog.Items;
                foreach (var selectedItem in selectedItems)
                    GdbPath = selectedItem.Path;

                OnPropertyChanged(nameof(GdbPath));
            }
        }
        #endregion
    }
}
