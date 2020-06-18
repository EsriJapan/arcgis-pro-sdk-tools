using ArcGIS.Desktop.Framework;

namespace ESRIJ.ArcGISPro
{
    internal class ModuleMain : ArcGIS.Desktop.Framework.Contracts.Module
    {
        private static ModuleMain _this = null;

        /// <summary>
        /// Retrieve the singleton instance to this module here
        /// </summary>
        public static ModuleMain Current
        {
            get
            {
                return _this ?? (_this = (ModuleMain)FrameworkApplication.FindModule("EJMesh_Module"));
            }
        }

        #region Overrides
        /// <summary>
        /// Called by Framework when ArcGIS Pro is closing
        /// </summary>
        /// <returns>False to prevent Pro from closing, otherwise True</returns>
        protected override bool CanUnload()
        {
            //TODO - add your business logic
            //return false to ~cancel~ Application close
            return true;
        }

        #endregion Overrides

    }
}
