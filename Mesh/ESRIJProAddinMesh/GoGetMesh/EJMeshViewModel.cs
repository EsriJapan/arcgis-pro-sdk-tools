using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;

namespace ESRIJ.ArcGISPro
{
    public class EJMeshViewModel : DockPane
    {
        private EJMeshCalculator _eJMeshCalculator = new EJMeshCalculator();

        #region 起動時
        /// <summary>
        /// コンストラクタ
        /// </summary>
        protected EJMeshViewModel()
        {
            
            RadioFirstMesh = true;
            _detectMesh = new RelayCommand(() => ExecuteDetectMesh(), () => true);
            _moveToMesh = new RelayCommand(() => ExecuteMoveToMesh(), () => true);
            _createMesh = new RelayCommand(() => ExecuteCreateMesh(), () => true);

            // プロパティ変更通知
            _eJMeshCalculator.PropertyChanged += RaisePropertyChanged;
        }
        #endregion

        #region デストラクタ
        /// <summary>
        /// デストラクタ
        /// </summary>
        ~EJMeshViewModel()
        {
            _eJMeshCalculator.PropertyChanged -= RaisePropertyChanged;
        }
        #endregion

        #region ドッキングウインドウイベント
        /// <summary>
        /// ドッキングウインドウの表示/非表示
        /// </summary>
        protected override void OnShow(bool isVisible)
        {
            // タブを閉じたら強調や項目の値をクリアする
            if (!isVisible)
            {
                _eJMeshCalculator.RemoveFromMapOverlay();
                _eJMeshCalculator.HighlightPolygon = null;
                
                if (FirstMesh != null)
                {
                    FirstMesh = null;
                    NotifyPropertyChanged(() => FirstMesh);
                }

                if (SecondMesh != null)
                {
                    SecondMesh = null;
                    NotifyPropertyChanged(() => SecondMesh);
                }

                if (ThirdMesh != null)
                {
                    ThirdMesh = null;
                    NotifyPropertyChanged(() => ThirdMesh);
                }

                Latitude = null;
                NotifyPropertyChanged(() => Latitude);

                Longitude = null;
                NotifyPropertyChanged(() => Longitude);
            }
        }
        #endregion

        #region プロパティ
        public string FirstMesh
        {
            get { return _eJMeshCalculator.FirstMesh; }
            set
            {
                _eJMeshCalculator.FirstMesh = value;
                NotifyPropertyChanged(() => FirstMesh);
            }
        }

        public string SecondMesh
        {
            get { return _eJMeshCalculator.SecondMesh; }
            set
            {
                _eJMeshCalculator.SecondMesh = value;
                NotifyPropertyChanged(() => SecondMesh);
            }
        }

        public string ThirdMesh
        {
            get { return _eJMeshCalculator.ThirdMesh; }
            set
            {
                _eJMeshCalculator.ThirdMesh = value;
                NotifyPropertyChanged(() => ThirdMesh);
            }
        }

        public string Latitude
        {
            get { return _eJMeshCalculator.Latitude; }
            set
            {
                _eJMeshCalculator.Latitude = value;
                NotifyPropertyChanged(() => Latitude);
            }
        }

        public string Longitude
        {
            get { return _eJMeshCalculator.Longitude; }
            set
            {
                _eJMeshCalculator.Longitude = value;
                NotifyPropertyChanged(() => Longitude);
            }
        }

        public bool RadioFirstMesh
        {
            get { return _eJMeshCalculator.RadioFirstMesh; }
            set
            {
                _eJMeshCalculator.RadioFirstMesh = value;
                NotifyPropertyChanged(() => RadioFirstMesh);
            }
        }

        public bool RadioSecondMesh
        {
            get { return _eJMeshCalculator.RadioSecondMesh; }
            set
            {
                _eJMeshCalculator.RadioSecondMesh = value;
                NotifyPropertyChanged(() => RadioSecondMesh);
            }
        }

        public bool RadioThirdMesh
        {
            get { return _eJMeshCalculator.RadioThirdMesh; }
            set
            {
                _eJMeshCalculator.RadioThirdMesh = value;
                NotifyPropertyChanged(() => RadioThirdMesh);
            }
        }
        
        /// <summary>
        /// DockPane のタイトル
        /// </summary>
        private string _heading = "";
        public string Heading
        {
            get { return _heading; }
            set
            {
                SetProperty(ref _heading, value, () => Heading);
            }
        }
        #endregion

        #region コマンド
        /// <summary>
        /// 計算ボタン
        /// </summary>
        private ICommand _detectMesh;
        public ICommand DetectMesh => _detectMesh;
        private void ExecuteDetectMesh()
        {
            _eJMeshCalculator.GetMesh();
        }

        /// <summary>
        /// 移動ボタン
        /// </summary>
        private ICommand _moveToMesh;
        public ICommand MoveToMesh => _moveToMesh;
        private void ExecuteMoveToMesh()
        {
            _eJMeshCalculator.Zoom2Mesh();
        }

        /// <summary>
        /// 作成ボタン
        /// </summary>
        private ICommand _createMesh;
        public ICommand CreateMesh => _createMesh;
        private MeshDialogViewModel _meshDialogViewModel;
        private void ExecuteCreateMesh()
        {
            // 地域メッシュ作成画面起動
            _meshDialogViewModel = new MeshDialogViewModel();
            _meshDialogViewModel.EJMeshCalculator = _eJMeshCalculator;
            _meshDialogViewModel.Open();
        }
        #endregion

        #region プロパティ変更通知
        private void RaisePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            NotifyPropertyChanged(e.PropertyName);
        }
        #endregion
    }
}
