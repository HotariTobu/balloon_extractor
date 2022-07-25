using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace balloon_collector
{
    internal class MWVM : SharedWPF.ViewModelBase
    {
        #region == ToPath ==

        private string _ToPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "balloon_pack");
        public string ToPath
        {
            get => _ToPath;
            set
            {
                _ToPath = value;
                RaisePropertyChanged();
            }
        }

        #endregion
    }
}
