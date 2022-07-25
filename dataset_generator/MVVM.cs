using System;

namespace dataset_generator
{
    internal class MVVM : SharedWPF.ViewModelBase
    {
        #region == SourcePath ==

        private string _SourcePath = "";
        public string SourcePath
        {
            get => _SourcePath;
            set
            {
                if (!_SourcePath.Equals(value))
                {
                    _SourcePath = value;
                    RaisePropertyChanged();
                }
            }
        }

        #endregion
        #region == Threshold ==

        private int _Threshold = 240;
        public int Threshold
        {
            get => _Threshold;
            set
            {
                if (value <= 0 | value > 255)
                {
                    AddError();
                }
                else
                {
                    ClearErrors();

                    _Threshold = value;
                    RaisePropertyChanged();
                }
            }
        }

        #endregion

        #region == PageIndex ==

        private int _PageIndex;
        public int PageIndex
        {
            get => _PageIndex;
            set
            {
                if (0 <= value && value < PageCount)
                {
                    _PageIndex = value;
                    RaisePropertyChanged();
                    OnPageIndexChanged?.Invoke();
                }
            }
        }

        public event Action? OnPageIndexChanged;

        #endregion
        #region == PageCount ==

        private int _PageCount;
        public int PageCount
        {
            get => _PageCount;
            set
            {
                if (_PageCount != value)
                {
                    _PageCount = value;
                    RaisePropertyChanged();
                }
            }
        }

        #endregion

        #region == IsExporting ==

        private bool _IsExporting;
        public bool IsExporting
        {
            get => _IsExporting;
            set
            {
                if (_IsExporting != value)
                {
                    _IsExporting = value;
                    RaisePropertyChanged();
                }
            }
        }

        #endregion
    }
}
