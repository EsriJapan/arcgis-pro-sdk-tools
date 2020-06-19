using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ESRIJ.ArcGISPro
{
    /// <summary>
    /// 文字列処理クラス
    /// </summary>
    public static class UtilString
    {
        public static int NumberOfCharacters(string str)
        {
            if (str != null)
            {
                return str.Length;
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// 文字列 -> 数値変換
        /// 空文字は０
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="str"></param>
        /// <param name="outval"></param>
        /// <returns></returns>
        public static bool TryParse<T>(string str, out T outval)
        {
            outval = default(T);    //0
            if (string.IsNullOrEmpty(str)) return true;

            try
            {
                var conv = TypeDescriptor.GetConverter(typeof(T));
                if (conv == null) return false;
                outval = (T)conv.ConvertFromString(str);
                return true;
            }
            catch
            {
                return false;
            }
        }

        // ↓ここ他のユーティリティクラス作って移す

        /// <summary>
        /// 指定したパスにディレクトリが存在しない場合
        /// すべてのディレクトリとサブディレクトリを作成します
        /// </summary>
        public static DirectoryInfo SafeCreateDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                return null;
            }
            return Directory.CreateDirectory(path);
        }
    }
}
