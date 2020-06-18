using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Core.Geoprocessing;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ESRIJ.ArcGISPro
{
    public class ClipManager
    {
        private FeatureLayer _featureLayer;
        private string _featureClassName;

        /// <summary>
        /// クリップ実行
        /// </summary>
        public async void RunClip()
        {
            if (DirectoryCheck())
            {
                var result = MessageBox.Show("選択したポリゴンでクリップしますか？", "確認",
                                             System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Information,
                                             System.Windows.MessageBoxResult.Yes);

                if (result == System.Windows.MessageBoxResult.No)
                {
                    return;
                }
                else
                {
                    
                    bool check = await CheckSpatialReference();

                    // マップの座標系とフィーチャークラスの座標系が異なっていたらエラー
                    if (check)
                    {
                        ExecuteClip();
                    }
                }
            }
        }

        /// <summary>
        /// クリアー処理
        /// </summary>
        public void RunClear()
        {
            ClipModule.RemoveFromMapOverlay();
            ClipModule.RemovePolygonForClip();
            ClipModule.ClearSelection();

            FrameworkApplication.SetCurrentToolAsync("esri_mapping_exploreTool");
        }

        /// <summary>
        /// クリップ格納先チェック
        /// </summary>
        private bool DirectoryCheck()
        {
            if (MapView.Active == null)
            {
                MessageBox.Show("マップを開いてください", "警告",
                                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning,
                                System.Windows.MessageBoxResult.Yes);
                return false ;
            }
            else if (ClipModule.OverlayObject == null || ClipModule.PolygonForClip == null)
            {
                MessageBox.Show("クリップ用のポリゴンを作図または選択してください", "警告",
                                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning,
                                System.Windows.MessageBoxResult.Yes);
                return false;
            }
            else if (ClipModule.SaveLocation.Text == null)
            {
                MessageBox.Show("格納先を指定してください", "警告",
                                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning,
                                System.Windows.MessageBoxResult.Yes);
                return false;
            }
            else if (!Directory.Exists(ClipModule.SaveLocation.Text))
            {
                if (CheckDataSource())
                {
                    MessageBox.Show("格納先が不正です。正しい格納先を入力してください", "警告",
                                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning,
                                System.Windows.MessageBoxResult.Yes);
                    return false;
                }
            }         

            return true;

        }

        /// <summary>
        /// クリップ格納先がGDBかSDEかのチェック
        /// </summary>
        private bool CheckDataSource()
        {
            string connection = System.IO.Path.GetFileName(ClipModule.SaveLocation.Text);
            string suffix = System.IO.Path.GetExtension(connection).ToLower();

            // sde の場合false
            if (suffix == ".sde")
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 出力フィーチャークラス名の設定
        /// </summary>
        private string SetFeatureClassName(string name)
        {
            DateTime dt = DateTime.Now;
            var time = dt.ToString("yyyyMMddHHmmss");
            return name + "_Clip_" + time;
        }

        /// <summary>
        /// フィーチャーレイヤー取得（SDE対応）
        /// </summary>
        private FeatureLayer GetFeatureLayerForSDE(string featureClassName)
        {
            // パスからフィーチャクラス名を取得する
            var pos = featureClassName.LastIndexOf("\\");
            var featureLayerName = featureClassName.Substring(pos + 1, featureClassName.Length - pos - 1);
            return MapView.Active.Map.FindLayers(featureLayerName).FirstOrDefault() as FeatureLayer;
        }

        /// <summary>
        /// フィーチャークラス名取得（SDE対応）
        /// </summary>
        private string GetFeatureClassNameForSDE(string featureClassName)
        {
            // SDEの場合、フィーチャークラス名に「CHIBA_SDE.DBO」がついてCHIBA_SDE.DBO.フィーチャークラス名のようになってしまうのでそれの対応
            if (featureClassName.Contains("."))
            {
                var pos = featureClassName.LastIndexOf(".");
                return featureClassName.Substring(pos + 1, featureClassName.Length - pos - 1);
            }
            else
            {
                return featureClassName;
            }
        }

        private bool CopyFeatureClass(BasicFeatureLayer layer)
        {
            var mapView = MapView.Active;

            try
            {
                // フィーチャークラス名の設定
                _featureClassName = SetFeatureClassName(layer.Name);

                // SDEの場合のフィーチャークラス名の設定
                _featureClassName = GetFeatureClassNameForSDE(_featureClassName);

                // フィーチャークラス作成
                Task<IGPResult> resultFeatureClass = ClipModule.ExecuteGeoprocessingTool("FeatureClassToFeatureClass_conversion",
                                                                                          Geoprocessing.MakeValueArray(layer,
                                                                                                                       ClipModule.SaveLocation.Text,
                                                                                                                       _featureClassName));

                if (resultFeatureClass.Result.IsFailed)
                {
                    return false;
                }

                _featureLayer = mapView.Map.FindLayers(_featureClassName).FirstOrDefault() as FeatureLayer;

                // SDE対応
                if (_featureLayer == null)
                {
                    _featureLayer = GetFeatureLayerForSDE(resultFeatureClass.Result.Values.FirstOrDefault());
                }

                return true;
            }
            catch (Exception)
            {
                throw;
            }

        }



        /// <summary>
        /// 座標系チェック
        /// </summary>
        private async Task<bool> CheckSpatialReference()
        {
            var mapView = MapView.Active;
            List<string> wrongSpatialReferenceList = new List<string>();

            try
            {
                await QueuedTask.Run(() =>
                {
                    // 各レイヤの座標系がマップの座標系と同じかチェック
                    foreach (var layer in ClipModule.GetLayers())
                    {
                        if (layer.SelectionCount == 0)
                            continue;

                        if (mapView.Map.SpatialReference.Name != layer.GetSpatialReference().Name)
                        {
                            wrongSpatialReferenceList.Add(layer.Name);
                        }
                    }
                });

                // レイヤの座標系がマップの座標系と異なっている場合、エラー
                if (wrongSpatialReferenceList.Count() > 0)
                {
                    StringBuilder sb = new StringBuilder();

                    sb.AppendLine("以下のフィーチャークラスとマップの座標系が異なります");

                    wrongSpatialReferenceList.ForEach(p => sb.AppendLine(p));



                    MessageBox.Show(sb.ToString(), "警告",
                                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning,
                                    System.Windows.MessageBoxResult.Yes);

                    return false;
                }

                return true;
            }
            catch (Exception)
            {
                MessageBox.Show("クリップの実行に失敗しました。", "エラー",
                                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error,
                                System.Windows.MessageBoxResult.Yes);

                return false;
            }
            
        }

        /// <summary>
        /// クリップ処理
        /// </summary>
        private async Task ClipFeatures(BasicFeatureLayer layer)
        {
            //クリップ用フィーチャークラス作成
            bool result = CopyFeatureClass(layer);

            if (result == false)
                throw new Exception();

            var annotationLayer = layer as AnnotationLayer;

            // アノテーションだったら処理対象外（CopyFeatureClassでコピー済みだから）
            if (annotationLayer == null)
            {
                var featureLayer = layer as FeatureLayer;

                await QueuedTask.Run(() =>
                {
                    var geometryType = featureLayer.GetFeatureClass().GetDefinition().GetShapeType().ToString();

                    // ポイントだったら処理対象外（CopyFeatureClassでコピー済みだから）
                    if (geometryType != "Point")
                    {
                        // クリップ処理
                        using (var rowCursor = layer.GetSelection().Search(null))
                        {
                            var editOperation = new EditOperation();

                            int i = 1;
                            while (rowCursor.MoveNext())
                            {
                                using (var row = rowCursor.Current)
                                {
                                    Feature feature = row as Feature;
                                    Geometry geo = feature.GetShape();
                                    bool isContain = GeometryEngine.Instance.Contains(ClipModule.PolygonForClip, geo);
                                    if (isContain == false)
                                    {
                                        editOperation.Clip(_featureLayer, i, ClipModule.PolygonForClip, ClipMode.PreserveArea);
                                    }
                                }
                                i++;
                            }

                            editOperation.Execute();
                        }

                        // クリップできなかったフィーチャ（対象エリアと交差するけど位置的にクリップできないもの）を削除
                        DeleteFeatures();
                    }
                    else if (geometryType == "Point")
                    {
                        // マップツールの仕様上、縮小した状態で範囲を選択すると範囲外のポイントも選択されてしまうので、それらを削除
                        DeleteFeatures();
                    }
                });

                await Project.Current.SaveEditsAsync();
            }
        }

        private void DeleteFeatures()
        {
            try
            {
                using (Geodatabase gdb = GetGdb(ClipModule.SaveLocation.Text))
                {
                    using (FeatureClass featureClass = gdb.OpenDataset<FeatureClass>(_featureClassName))
                    {
                        var deleteOperation = new EditOperation();

                        using (var rowCursor = featureClass.Search(null))
                        {
                            List<long> rows = new List<long>();

                            while (rowCursor.MoveNext())
                            {
                                using (var row = rowCursor.Current)
                                {
                                    Feature feature = row as Feature;
                                    Geometry geo = feature.GetShape();

                                    // クリップ用のフィーチャに含まれないフィーチャは削除
                                    bool isContain = GeometryEngine.Instance.Contains(ClipModule.PolygonForClip, geo);
                                    if (isContain == false)
                                    {
                                        rows.Add(row.GetObjectID());
                                    }
                                }
                            }
                            deleteOperation.Delete(featureClass, rows);
                            deleteOperation.Execute();
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// クリップ実行
        /// </summary>
        private async void ExecuteClip()
        {
            try
            {
                // 対象となるレイヤーの総数
                var total = ClipModule.GetLayers().Where(c => c.SelectionCount > 0).Count();
                using (var progress = new ProgressDialog("クリップ実行中です", "キャンセルします", (uint)total, true))
                {
                    var cps = new CancelableProgressorSource(progress);
                    progress.Show();

                    await QueuedTask.Run(async () =>
                    {
                        cps.Progressor.Max = (uint)total;
                        
                        var layers = ClipModule.GetLayers().Where(c => c.SelectionCount > 0);
                        
                        foreach (var layer in layers)
                        {
                            if (cps.Progressor.CancellationToken.IsCancellationRequested)
                                break;

                            cps.Progressor.Message = layer.Name + "を処理中です ";

                            await ClipFeatures(layer);

                            cps.Progressor.Value += 1;
                            cps.Progressor.Status = (cps.Progressor.Value * 100 / cps.Progressor.Max) + " % 完了";
                            Task.Delay(1000).Wait();
                        }
                    }, cps.Progressor);
                }

                RunClear();
            }
            catch (Exception)
            {
                MessageBox.Show("クリップの実行に失敗しました。", "エラー",
                                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error,
                                System.Windows.MessageBoxResult.Yes);
            }
            finally
            {
                ClipModule.RemoveFromMapOverlay();
                ClipModule.RemovePolygonForClip();
            }

        }

        /// <summary>
        /// GDB/SDE取得用メソッド
        /// </summary>
        private Geodatabase GetGdb(string path)
        {
            // 指定した格納先がSDEの場合
            if (!CheckDataSource())
            {
                return new Geodatabase(new DatabaseConnectionFile(new Uri(path)));
            }
            else
            {
                return new Geodatabase(new FileGeodatabaseConnectionPath(new Uri(path)));
            }
        }
    }
}
