using Microsoft.WindowsAPICodePack.Dialogs;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace dataset_generator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MVVM VM;
        private List<string> SourcePaths = new List<string>();

        private CommonOpenFileDialog FileDialog = new CommonOpenFileDialog { IsFolderPicker = true, };

        public MainWindow()
        {
            InitializeComponent();

            VM = (MVVM)DataContext;
            VM.OnPageIndexChanged += VM_OnPageIndexChanged;
        }

        private void VM_OnPageIndexChanged()
        {
            VM.SourcePath = SourcePaths[VM.PageIndex];
        }

        private void CollectFilePaths(IEnumerable<string> paths)
        {
            foreach (string path in paths)
            {
                if (File.Exists(path) && !Path.GetFileNameWithoutExtension(path).EndsWith(Exporter.MaskSuffix))
                {
                    SourcePaths.Add(path);
                }
                else if (Directory.Exists(path))
                {
                    CollectFilePaths(Directory.EnumerateFileSystemEntries(path));
                }
            }
        }

        #region == Drag And Drop ==

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }

            e.Handled = true;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                CollectFilePaths((string[])e.Data.GetData(DataFormats.FileDrop));

                int newPageIndex = VM.PageCount;
                VM.PageCount = SourcePaths.Count;
                VM.PageIndex = newPageIndex;
            }
        }

        #endregion

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.A:
                    VM.PageIndex++;
                    break;
                case Key.D:
                    VM.PageIndex--;
                    break;
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (FileDialog.ShowDialog(this) == CommonFileDialogResult.Ok)
            {
                VM.SourcePath = "";
                VM.IsExporting = true;
                //await Task.Run(() => Exporter.ExportAll(SourcePaths, FileDialog.FileName));
                VM.IsExporting = false;
            }
        }
    }
}
