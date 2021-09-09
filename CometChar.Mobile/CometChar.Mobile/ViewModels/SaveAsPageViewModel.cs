using CometChar.Mobile.Services;
using CometChar.Mobile.Views;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace CometChar.Mobile.ViewModels
{
    public class SaveAsPageViewModel : BaseViewModel
    {
        IExternalStorage _exstor = DependencyService.Get<IExternalStorage>();

        public ObservableCollection<FolderItem> Directories { get => _directories; private set { } }
        ObservableCollection<FolderItem> _directories = new ObservableCollection<FolderItem>();

        string _currentDirectory;
        public string CurrentDirectory { get => _currentDirectory; set { _currentDirectory = value; OnPropertyChanged("CurrentDirectory"); } }

        string _filename = "output.z64";
        public string FileName { get => _filename; set { _filename = value; } }

        public SaveAsPageViewModel()
        {
            OpenWebCommand = new Command(async () => await Browser.OpenAsync("https://aka.ms/xamarin-quickstart"));
            PushDirectoryCommand = new Command<ItemTappedEventArgs>((e) => { PushDirectory((FolderItem)e.Item); });
            ConfirmCommand = new Command(() => Confirm());
            CancelCommand = new Command(() => Cancel());
            PushDirectory(new FolderItem() { Path = _exstor.GetPath() });
            CdStorageFolder = new Command(() => GoToDirectory(new FolderItem() { Path = "/storage" }));
            CdAppFilesFolderCommand = new Command(() => GoToDirectory(new FolderItem() { Path = _exstor.GetPath() }));
            BindingBase.EnableCollectionSynchronization(Directories, null, DirsCallback);
        }

        void DirsCallback(IEnumerable collection, object context, Action accessMethod, bool writeAccess)
        {
            // `lock` ensures that only one thread access the collection at a time
            lock (collection)
            {
                accessMethod?.Invoke();
            }
        }

        public void ReadDirectories()
        {
            _directories.Clear();
            _directories.Add(new FolderItem() { Path = "..", Type = "Go Up", Image = "folder_up" });
            try
            {
                string[] dirs = Directory.GetDirectories(_currentDirectory);
                foreach (string dir in dirs)
                {
                    string newName = new DirectoryInfo(dir).Name;
                    _directories.Add(new FolderItem() { Path = newName, Type = "Directory", Image = "folder_normal" });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Something horrible happened while listing directories");
            }

        }

        public void PushDirectory(FolderItem fi)
        {
            if (fi.Path == "..")
            {
                _currentDirectory = Directory.GetParent(_currentDirectory).FullName;
                ReadDirectories();
                OnPropertyChanged("CurrentDirectory");
                return;
            }

            if (_currentDirectory == "/") _currentDirectory = "";
            _currentDirectory += fi.Path.StartsWith("/") ? fi.Path : $"/{fi.Path}";
            ReadDirectories();
            OnPropertyChanged("CurrentDirectory");

        }

        public void GoToDirectory(FolderItem fi)
        {
            _currentDirectory = "/";
            PushDirectory(fi);
        }

        public async Task Confirm()
        {
            try
            {
                File.WriteAllText(Path.Combine(_currentDirectory, "dummy"), "Can I Be Written?");
                File.Delete(Path.Combine(_currentDirectory, "dummy"));
                if (File.Exists(Path.Combine(_currentDirectory, _filename)))
                {
                    bool answer = await Application.Current.MainPage.DisplayAlert("This file exists!", "The chosen filename already exists. Overwrite?", "Yes", "No");
                    if (!answer) return;
                }

                MessagingCenter.Send("SaveAsModal", "Path", Path.Combine(_currentDirectory, _filename));
            }
            catch (UnauthorizedAccessException ex)
            {
                await Application.Current.MainPage.DisplayAlert("No can do!", "The app cannot write to the selected folder. Choose another one!", "Alright");
            }

        }

        public void Cancel()
        {
            MessagingCenter.Send<string, string>("SaveAsModal", "Path", null);
        }

        public ICommand CdAppFilesFolderCommand { get; }

        public ICommand CdStorageFolder { get; }
        public ICommand PushDirectoryCommand { get; }

        public ICommand ConfirmCommand { get; }

        public ICommand CancelCommand { get; }

        public ICommand DirectoryUpCommand { get; }

        public ICommand OpenWebCommand { get; }
    }

    public class FolderItem
    {
        public string Path { get; set; }
        public string Type { get; set; }
        public string Image { get; set; }
    }
}