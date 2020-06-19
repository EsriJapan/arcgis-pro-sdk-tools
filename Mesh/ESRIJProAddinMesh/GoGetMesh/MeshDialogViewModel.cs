using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Catalog;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Core.Geoprocessing;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Internal.Mapping.Locate;
using ArcGIS.Desktop.Mapping;

namespace ESRIJ.ArcGISPro
{
    public partial class MeshDialogViewModel : BindBase
    {
        private MeshCreator _meshCreator = new MeshCreator();

        #region 起動時
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public MeshDialogViewModel()
        {
            RadioNew = true;
            _openItemCmd = new RelayCommand(() => OpenItemDialog(), () => true);
            _makeMeshCmd = new RelayCommand(() => ExecuteMakeMesh(), () => true);

            // プロパティ変更通知
            _meshCreator.PropertyChanged += RaisePropertyChanged;
        }
        #endregion

        #region デストラクタ
        /// <summary>
        /// デストラクタ
        /// </summary>
        ~MeshDialogViewModel()
        {
            _meshCreator.PropertyChanged -= RaisePropertyChanged;
        }
        #endregion

        #region プロパティ
        //private EJMeshCalculator _eJMeshCalculator;
        public EJMeshCalculator EJMeshCalculator
        {
            get { return _meshCreator.EJMeshCalculator; }
            set
            {
                _meshCreator.EJMeshCalculator = value;
            }
        }

        public bool RadioNew
        {
            get { return _meshCreator.RadioNew; }
            set
            {
                _meshCreator.RadioNew = value;
                OnPropertyChanged(nameof(RadioNew));
            }
        }

        public bool RadioAdd
        {
            get { return _meshCreator.RadioAdd; }
            set
            {
                _meshCreator.RadioAdd = value;
                OnPropertyChanged(nameof(RadioAdd));
            }
        }

        public string GdbPath
        {
            get { return _meshCreator.GdbPath; }
            set
            {
                _meshCreator.GdbPath = value;
                OnPropertyChanged(nameof(GdbPath));                    
            }
        }

        public string FeatureClassPath
        {
            get { return _meshCreator.FeatureClassPath; }
            set
            {
                _meshCreator.FeatureClassPath = value;
                OnPropertyChanged(nameof(FeatureClassPath));
            }
        }

        /// <summary>
        /// フィールド コンボ ボックス
        /// </summary>
        private List<String> _fields = new List<String>();
        public List<String> Fields
        {
            get { return _meshCreator.Fields; }
            set
            {
                _meshCreator.Fields = value;
                OnPropertyChanged(nameof(Fields));
            }
        }

        /// <summary>
        /// フィールド コンボ ボックスで選択しているフィールド
        /// </summary>
        /// 
        public string SelectedField
        {
            get { return _meshCreator.SelectedField; }
            set
            {
                _meshCreator.SelectedField = value;
                OnPropertyChanged(nameof(SelectedField));
            }
        }
        #endregion

        #region コマンド
        /// <summary>
        /// ダイアログ起動
        /// </summary>
        private ICommand _openItemCmd;
        public ICommand OpenItemCmd => _openItemCmd;
        private void OpenItemDialog()
        {
            _meshCreator.ShowDialog();
        }

        /// <summary>
        /// 地域メッシュ作成
        /// </summary>
        private ICommand _makeMeshCmd;
        public ICommand MakeMeshCmd => _makeMeshCmd;
        
        private void ExecuteMakeMesh()
        {
            _meshCreator.MakeMesh();
        }
        #endregion

        #region 地域メッシュ作成画面
        /// <summary>
        /// 地域メッシュ作成画面起動
        /// </summary>
        private MeshDialog _meshDialog = null;
        private static bool _isOpen = false;
        public void Open()
        {
            if (_isOpen)
                return;

            _meshDialog = new MeshDialog();
            _isOpen = true;
            _meshDialog.Closing += MeshDialogClose;
            _meshDialog.DataContext = this;
            
            _meshDialog.Owner = FrameworkApplication.Current.MainWindow;
            _meshDialog.Closed += (o, e) => { _meshDialog = null; };

            // 最大化はさせない
            _meshDialog.ShowMaxRestoreButton = false;

            // モードレス
            _meshDialog.Show();

            // モーダル
            //_meshDialog.IsModal();
            //_meshDialog.ShowDialog();
        }
        
        /// <summary>
        /// 地域メッシュ作成画面クローズイベント
        /// </summary>
        private void MeshDialogClose(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _meshDialog = null;
            _isOpen = false;
        }
        #endregion

        #region プロパティ変更通知
        private void RaisePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e.PropertyName);
        }
        #endregion
    }
}
