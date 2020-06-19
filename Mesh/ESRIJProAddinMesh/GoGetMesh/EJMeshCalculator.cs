using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ESRIJ.ArcGISPro
{
    public class EJMeshCalculator : BindBase
    {
        #region プロパティ
        public string FirstMesh { get; set; }
        public string SecondMesh { get; set; }
        public string ThirdMesh { get; set; }
        public string Latitude { get; set; }
        
        public string Longitude { get; set; }
        public bool RadioFirstMesh { get; set; }
        public bool RadioSecondMesh { get; set; }
        public bool RadioThirdMesh { get; set; }

        public Dictionary<string, Polygon> HighlightPolygon { get; set; }

        #endregion

        #region ビジネスロジック
        /// <summary>
        /// 地域コードから緯度経度を取得
        /// </summary>
        public void GetMesh()
        {
            var mapView = MapView.Active;
            if (mapView == null)
                return;
            
            // 強調削除
            RemoveFromMapOverlay();

            try
            {
                // 一次メッシュ
                if (RadioFirstMesh == true)
                {
                    if (UtilString.NumberOfCharacters(FirstMesh) == 4 &&
                        UtilString.TryParse<int>(FirstMesh, out int i))
                    {
                        // 緯度経度計算
                        LocateMesh(FirstMesh,1);

                        // メッシュ作成
                        CaluclateMesh(FirstMesh,1);
                    }
                    else
                    {
                        ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("4桁の地域メッシュコードを入力してください。", "警告", 
                                                                          System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning, 
                                                                          System.Windows.MessageBoxResult.Yes);
                    }
                }
                // 二次メッシュ
                else if (RadioSecondMesh == true)
                {
                    if (UtilString.NumberOfCharacters(SecondMesh) == 6 &&
                        UtilString.TryParse<int>(SecondMesh, out int i))
                    {
                        // 緯度経度計算
                        LocateMesh(SecondMesh,2);

                        // メッシュ作成
                        CaluclateMesh(SecondMesh, 2);
                    }
                    else
                    {
                        ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("6桁の地域メッシュコードを入力してください。", "警告", 
                                                                         System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning, 
                                                                         System.Windows.MessageBoxResult.Yes);
                    }

                }
                // 三次メッシュ
                else
                {
                    if (UtilString.NumberOfCharacters(ThirdMesh) == 8 &&
                        UtilString.TryParse<int>(ThirdMesh, out int i))
                    {
                        // 緯度経度計算
                        LocateMesh(ThirdMesh,3);

                        // メッシュ作成
                        CaluclateMesh(ThirdMesh, 3);
                    }
                    else
                    {
                        ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("8桁の地域メッシュコードを入力してください。", "警告", 
                                                                         System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning, 
                                                                         System.Windows.MessageBoxResult.Yes);
                    }
                }

                // 緯度経度のプロパティ更新
                OnPropertyChanged(nameof(Latitude));
                OnPropertyChanged(nameof(Longitude));
            }
            catch (Exception)
            {
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("地域メッシュの計算に失敗しました。", "エラー", 
                                                                 System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error, 
                                                                 System.Windows.MessageBoxResult.Yes);

                
            }
        }

        /// <summary>
        /// 緯度経度を計算するメソッド
        /// </summary>
        private void LocateMesh(string meshCode, int level)
        {
            try
            {
                // 一次メッシュ計算
                if (level == 1)
                {
                    var codeFirstTwo = meshCode.Substring(0, 2);
                    var codeLastTwo = meshCode.Substring(2, 2);
                    var lat = Double.Parse(codeFirstTwo) * 2 / 3;
                    var lon = Double.Parse(codeLastTwo) + 100;
                    Latitude = lat.ToString();
                    Longitude = lon.ToString();
                }
                // 二次メッシュ計算
                else if (level == 2)
                {
                    // 一次メッシュの計算を行う
                    LocateMesh(meshCode, 1);

                    var codeFirst = meshCode.Substring(4, 1);
                    var codeLast = meshCode.Substring(5, 1);
                    var lat = Double.Parse(Latitude) + (Double.Parse(codeFirst) * 2 / 3 / 8);
                    var lon = Double.Parse(Longitude) + (Double.Parse(codeLast) / 8);
                    Latitude = lat.ToString();
                    Longitude = lon.ToString();
                }
                // 三次メッシュ計算
                else
                {
                    // 二次メッシュの計算を行う
                    LocateMesh(meshCode, 2);

                    var codeFirst = meshCode.Substring(6, 1);
                    var codeLast = meshCode.Substring(7, 1);
                    var lat = Double.Parse(Latitude) + (Double.Parse(codeFirst) * 2 / 3 / 8 / 10);
                    var lon = Double.Parse(Longitude) + (Double.Parse(codeLast) / 8 / 10);
                    Latitude = lat.ToString();
                    Longitude = lon.ToString();
                }
            }
            catch (ArithmeticException)
            {
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("地域メッシュコードが不正です。", "エラー", 
                                                                 System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error, 
                                                                 System.Windows.MessageBoxResult.Yes);

                
            }
            catch (Exception)
            {
                throw;
            }

        }

        /// <summary>
        /// 地域メッシュにズーム
        /// </summary>
        public void Zoom2Mesh()
        {
            var mapView = MapView.Active;
            if (mapView == null)
                return;

            // 地域メッシュ強調処理
            GetMesh();

            try
            {
                // 強調した地域メッシュにズーム
                if (HighlightPolygon != null)
                {
                    QueuedTask.Run(() =>
                    {
                        MapView.Active.ZoomTo(HighlightPolygon.FirstOrDefault().Value, TimeSpan.Zero);
                    });
                }
            }
            catch (Exception )
            {
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("地域メッシュコードへの移動に失敗しました。", "エラー",
                                                                 System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error,
                                                                 System.Windows.MessageBoxResult.Yes);

                
            }
            
        }

        /// <summary>
        /// 地域メッシュの大きさを計算
        /// </summary>
        private void CaluclateMesh(string meshCode, int level)
        {
            try
            {
                double lat_bottomLeft = Double.Parse(Latitude);
                double lon_bottomLeft = Double.Parse(Longitude);
                double hight;
                double width;

                if (level == 1)
                {
                    hight = 2.0 / 3.0;
                    width = 1.0;
                }
                else if (level == 2)
                {
                    hight = 2.0 / 3.0 / 8.0;
                    width = 1.0 / 8.0;
                }
                else
                {
                    hight = 2.0 / 3.0 / 8.0 / 10.0;
                    width = 1.0 / 8.0 / 10.0;
                }

                var lat_topLeft = lat_bottomLeft + hight;
                var lon_topLeft = lon_bottomLeft;

                var lat_topRight = lat_bottomLeft + hight;
                var lon_lat_topRight = lon_bottomLeft + width;

                var lat_bottomRight = lat_bottomLeft;
                var lon_lat_bottomRight = lon_bottomLeft + width;

                // ポリゴン作成
                MapPoint ptBottomLeft = MapPointBuilder.CreateMapPoint(lon_bottomLeft, lat_bottomLeft);
                MapPoint ptTopLeft = MapPointBuilder.CreateMapPoint(lon_topLeft, lat_topLeft);
                MapPoint ptTopRight = MapPointBuilder.CreateMapPoint(lon_lat_topRight, lat_topRight);
                MapPoint ptBottomRight = MapPointBuilder.CreateMapPoint(lon_lat_bottomRight, lat_bottomRight);
                List<MapPoint> list = new List<MapPoint>() { ptBottomLeft, ptTopLeft, ptTopRight, ptBottomRight };

                HighlightPolygon = new Dictionary<string, Polygon>();
                HighlightPolygon.Add(meshCode, PolygonBuilder.CreatePolygon(list, SpatialReferences.WGS84));

                // 強調処理
                HighlightMesh();
            }
            catch (System.ArgumentNullException )
            {
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("地域メッシュの計算に失敗しました。", "エラー",
                                                                 System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error,
                                                                 System.Windows.MessageBoxResult.Yes);

                
            }
            catch (Exception)
            {
                throw;
            }
            
        }

        /// <summary>
        /// 既存のグラフィックを削除
        /// </summary>
        private static System.IDisposable _overlayObject = null;
        public void RemoveFromMapOverlay()
        {
            if (_overlayObject != null)
            {
                _overlayObject.Dispose();
                _overlayObject = null;
                HighlightPolygon = null;
            }
        }

        /// <summary>
        /// 地域メッシュを強調
        /// </summary>
        private void HighlightMesh()
        {
            try
            {
                var polygonGraphic = new CIMPolygonGraphic();
                polygonGraphic.Polygon = HighlightPolygon.FirstOrDefault().Value;

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
                    _overlayObject = MapView.Active.AddOverlay(polygonGraphic);
                });
            }
            catch (System.ArgumentNullException )
            {
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("地域メッシュの計算に失敗しました。", "エラー",
                                                                 System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error,
                                                                 System.Windows.MessageBoxResult.Yes);

                
            }
            catch (Exception)
            {
                throw;
            }
        }
        #endregion
    }
}
