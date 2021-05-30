using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AppFindAndReplace.Commands;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace AppFindAndReplace.ViewModels
{
    class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private BackgroundWorker _bw;
        public ObservableCollection<FileContent> _collectionFoundFiles { get; set; }
        public ObservableCollection<FileContent> CollectionFoundFiles
        {
            get => _collectionFoundFiles;
            set
            {
                _collectionFoundFiles = value;
                OnPropertyChanged(nameof(CollectionFoundFiles));
            }
        }

        public MainViewModel()
        {
            _collectionFoundFiles = new ObservableCollection<FileContent>();
            _bw = new BackgroundWorker()
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            _bw.DoWork += BwFileDoWork;
            _bw.RunWorkerCompleted += BwFileRunWorkerCompleted;
            _bw.ProgressChanged += BwFileProgressChanged;
        }

        private void BwFileDoWork(object sender, DoWorkEventArgs e)
        {
            var files = GetFilteredFilesPath();
            if (files.Count() == 0)
                e.Cancel = true;

            for (int i = 0; i < files.Count(); i++)
            {
                string file = files.ElementAt(i);
                string textFile = File.ReadAllText(file);
                int countSubText = (textFile.Length - textFile.Replace(FindSubstring, "").Length) / FindSubstring.Length;

                if (_bw.CancellationPending == true)
                {
                    e.Cancel = true;
                    break;
                }
                if (countSubText != 0)
                {
                    if ((bool)e.Argument) // if press button replace
                    {
                        textFile = textFile.Replace(FindSubstring, ReplaceSubstring);
                        File.WriteAllText(file, textFile);
                    }

                    _bw.ReportProgress((int)((i + 1) / (float)files.Count() * 100), new FileContent { FilePath = file, FileName = System.IO.Path.GetFileName(file), Matches = countSubText });
                }
                else
                {
                    _bw.ReportProgress((int)((i + 1) / (float)files.Count() * 100));
                }
                Thread.Sleep(500);
            }
        }
        private IEnumerable<string> GetFilteredFilesPath()
        {
            var subDirections = (IncludeSubDirs == true) ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            string[] filesMask = System.IO.Directory.GetFiles(FilePath, FileMask, subDirections);
            if (string.IsNullOrWhiteSpace(ExcludeFileMask) == false)
            {
                string[] excludeFilesMask = System.IO.Directory.GetFiles(FilePath, ExcludeFileMask, subDirections);
                return filesMask.Except(excludeFilesMask).AsEnumerable();
            }
            return filesMask.AsEnumerable();
        }

        private void BwFileRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            MessageBox.Show("Completed");
        }

        private void BwFileProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if ((FileContent)e.UserState != null)
                CollectionFoundFiles.Add((FileContent)e.UserState);
            Progress = e.ProgressPercentage;
        }

        private int _progress;
        public int Progress
        {
            get => _progress;
            set
            {
                _progress = value;
                OnPropertyChanged(nameof(Progress));
            }
        }

        private string _filePath;
        public string FilePath
        {
            get => _filePath;
            set
            {
                _filePath = value;
                OnPropertyChanged(nameof(FilePath));
            }
        }

        private string _fileMask;
        public string FileMask
        {
            get => _fileMask;
            set
            {
                _fileMask = value;
                OnPropertyChanged(nameof(FileMask));
            }
        }

        private string _excludeFileMask;
        public string ExcludeFileMask
        {
            get => _excludeFileMask;
            set
            {
                _excludeFileMask = value;
                OnPropertyChanged(nameof(ExcludeFileMask));
            }
        }

        private bool _includeSubDirs;
        public bool IncludeSubDirs
        {
            get => _includeSubDirs;
            set
            {
                _includeSubDirs = value;
                OnPropertyChanged(nameof(IncludeSubDirs));
            }
        }

        private string _findSubstring;
        public string FindSubstring
        {
            get => _findSubstring;
            set
            {
                _findSubstring = value;
                OnPropertyChanged(nameof(FindSubstring));
            }
        }

        private string _replaceSubstring;
        public string ReplaceSubstring
        {
            get => _replaceSubstring;
            set
            {
                _replaceSubstring = value;
                OnPropertyChanged(nameof(ReplaceSubstring));
            }
        }

        public ICommand OpenFolderCommand => new RelayCommand(() =>
        {
            CommonOpenFileDialog dlg = new CommonOpenFileDialog();
            dlg.InitialDirectory = "C:\\";
            dlg.IsFolderPicker = true;
            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
                FilePath = dlg.FileName;
        }, () => true);

        public ICommand CancelCommand
        {
            get => new RelayCommand(() =>
            {
                _bw.CancelAsync();
            }, () => _bw.IsBusy);
        }

        public ICommand FindDataCommand => new RelayCommand(() =>
        {
            CollectionFoundFiles.Clear();
            _bw.RunWorkerAsync(false);
        }, () => string.IsNullOrWhiteSpace(FilePath) == false &&
                 string.IsNullOrWhiteSpace(FindSubstring) == false &&
                 string.IsNullOrWhiteSpace(FileMask) == false &&
                 _bw.IsBusy == false);

        public ICommand ReplaceDataCommand => new RelayCommand(() =>
        {
            CollectionFoundFiles.Clear();
            _bw.RunWorkerAsync(true);
        }, () => string.IsNullOrWhiteSpace(FilePath) == false &&
                 string.IsNullOrWhiteSpace(FileMask) == false &&
                 string.IsNullOrWhiteSpace(ReplaceSubstring) == false &&
                 string.IsNullOrWhiteSpace(FindSubstring) == false &&
                 _bw.IsBusy == false);

        public class FileContent
        {
            public string FilePath { get; set; }
            public string FileName { get; set; }
            public int Matches { get; set; }

        }
    }
}
