using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Events;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Core.Geoprocessing;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Events;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;


namespace ESRIJ.ArcGISPro
{
    public class ClipEvents
    {
        /// <summary>
        /// イベント用変数
        /// </summary>
        private static SubscriptionToken _mapSelectionChangedEvent = null;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public ClipEvents()
        {
            ActiveToolChangedEvent.Subscribe(OnActiveToolChanged);
            ActiveMapViewChangedEvent.Subscribe(OnActiveMapViewChanged);
        }

        /// <summary>
        /// マップツール起動処理
        /// </summary>
        public void ActivateDrawingTool()
        {
            var cmd = FrameworkApplication.GetPlugInWrapper(ClipModule.MapToolID) as ICommand;
            if (cmd.CanExecute(null))
                cmd.Execute(null);
        }

        /// <summary>
        /// クリップ対象範囲にあるフィーチャの検索
        /// </summary>
        private async void IdentifyFeatures()
        {
            var mapView = MapView.Active;
            if (mapView == null)
                return;

            try
            {
                // 「ポリゴン」ボタンを選択してクリップする場合
                if (ClipModule.MapToolID == "EJClip_SelectPolygonMapTool")
                {
                    await QueuedTask.Run(async () =>
                    {
                        var selection = mapView.Map.GetSelection();

                        if (selection.Count == 0)
                        {
                            return;
                        }

                        var featureLayer = selection.FirstOrDefault().Key as FeatureLayer;
                        if (featureLayer == null)
                            return;

                        var geometryType = featureLayer.GetFeatureClass().GetDefinition().GetShapeType().ToString();

                        // 選択したフィーチャがポリゴンの場合
                        if (geometryType == "Polygon")
                        {
                            if (selection.Count > 1)
                            {
                                MessageBox.Show("複数のポリゴンを選択したので、一番上の" + selection.FirstOrDefault().Key.Name + "を選択します。", "情報",
                                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information,
                                    System.Windows.MessageBoxResult.Yes);
                            }

                            using (var progress = new ProgressDialog("フィーチャを選択中です"))
                            {
                                progress.Show();

                                // 選択したポリゴンのオブジェクトIDを取得
                                QueryFilter queryFilter = new QueryFilter
                                {
                                    WhereClause = "ObjectId =" + featureLayer.GetSelection().GetObjectIDs().First(),
                                };

                                // 選択したポリゴンの範囲内にあるフィーチャを検索
                                using (var rowCursor = featureLayer.Search(queryFilter))
                                {
                                    rowCursor.MoveNext();

                                    using (var row = rowCursor.Current)
                                    {
                                        Feature feature = row as Feature;
                                        Geometry geo = feature.GetShape();

                                        ClipModule.PolygonForClip = geo;
                                        ClipModule.PolygonLayerForClip = featureLayer;

                                        // 一旦マップセレクション変更を解除
                                        MapSelectionChangedEvent.Unsubscribe(_mapSelectionChangedEvent);
                                        _mapSelectionChangedEvent = null;

                                        // 属性検索実行
                                        await SelectFeatures();
                                    }
                                }
                            }

                            // 選択範囲強調
                            CreateHighlight();
                        }
                        
                    });
                }
                else if (ClipModule.MapToolID == "EJClip_RectanbleMapTool" || ClipModule.MapToolID == "EJClip_PolygonMapTool")
                {
                    // 選択範囲強調
                    CreateHighlight();
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (_mapSelectionChangedEvent == null)
                {
                    _mapSelectionChangedEvent = MapSelectionChangedEvent.Subscribe(OnMapSelectionChanged);
                }
            }        
        }

        /// <summary>
        /// マップセレクション変更イベント
        /// </summary>
        private void OnMapSelectionChanged(MapSelectionChangedEventArgs args)
        {
            ClipModule.RemoveFromMapOverlay();

            try
            {
                IdentifyFeatures();
            }
            catch (Exception)
            {
                MessageBox.Show("クリップ範囲の指定に失敗しました。", "エラー",
                                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error,
                                System.Windows.MessageBoxResult.Yes);
            }
        }

        /// <summary>
        /// 属性検索実行処理
        /// </summary>
        private async Task SelectFeatures()
        {
            foreach (var layer in ClipModule.GetLayers())
            {
                IGPResult resultFeatureClass = await ClipModule.ExecuteGeoprocessingTool("SelectLayerByLocation_management",
                                                                                         Geoprocessing.MakeValueArray(layer,
                                                                                                                      "INTERSECT",
                                                                                                                      ClipModule.PolygonLayerForClip));
            }
        }

        /// <summary>
        /// クリップ範囲強調処理
        /// </summary>
        private void CreateHighlight()
        {
            try
            {
                // グラフィック作成
                var polygonGraphic = new CIMPolygonGraphic();
                polygonGraphic.Polygon = ClipModule.PolygonForClip as Polygon;

                // シンボル作成
                CIMStroke outline = SymbolFactory.Instance.ConstructStroke(ColorFactory.Instance.RedRGB,
                                                                           5.0,
                                                                           SimpleLineStyle.Solid);

                CIMPolygonSymbol polygonSymbol = SymbolFactory.Instance.ConstructPolygonSymbol(ColorFactory.Instance.RedRGB,
                                                                                               SimpleFillStyle.Null,
                                                                                               outline);
                polygonGraphic.Symbol = polygonSymbol.MakeSymbolReference();

                QueuedTask.Run(() =>
                {
                    // グラフィックをマップビューに追加
                    ClipModule.OverlayObject = MapView.Active.AddOverlay(polygonGraphic);
                });
                
            }
            catch (Exception)
            {
                throw;
            }
            
        }

        /// <summary>
        /// アクティブなツールが変更された際の処理
        /// </summary>
        private void OnActiveToolChanged(ArcGIS.Desktop.Framework.Events.ToolEventArgs args)
        {
            string newTool = args.CurrentID;

            if (_mapSelectionChangedEvent == null)
            {
                if (newTool == "EJClip_RectanbleMapTool" ||
                    newTool == "EJClip_PolygonMapTool" ||
                    newTool == "EJClip_SelectPolygonMapTool")
                {
                    _mapSelectionChangedEvent = MapSelectionChangedEvent.Subscribe(OnMapSelectionChanged);
                }
            }
            else
            {
                if (newTool != "EJClip_RectanbleMapTool" &&
                    newTool != "EJClip_PolygonMapTool" &&
                    newTool != "EJClip_SelectPolygonMapTool")
                {
                    MapSelectionChangedEvent.Unsubscribe(_mapSelectionChangedEvent);
                    _mapSelectionChangedEvent = null;
                }
            }
        }

        /// <summary>
        /// アクティブマップビュー変更処理
        /// </summary>
        private void OnActiveMapViewChanged(ActiveMapViewChangedEventArgs args)
        {
            ClipModule.RemoveFromMapOverlay();
            ClipModule.RemovePolygonForClip();
            ClipModule.ClearSelection();
            
            if (_mapSelectionChangedEvent == null)
            {
                _mapSelectionChangedEvent = MapSelectionChangedEvent.Subscribe(OnMapSelectionChanged);
            }
        }
    }
}
