using System.Windows.Input;
using Android.Content;
using CommunityToolkit.Maui.Storage;

namespace CometChar.Mobile.ViewModels
{
    public class PatchROMViewModel : BaseViewModel {
        string _statusText = "Ready";
        public string StatusText
        {
            get => _statusText;
            set
            {
                _statusText = value;
                OnPropertyChanged("StatusText");
            }
        }

        bool _isPatching = false;
        public bool IsPatching
        {
            get => _isPatching;
            set
            {
                _isPatching = value;
                OnPropertyChanged("IsPatching");
            }
        }

        string _romFilepath;

        string _cmtpFilepath;

        string _savedRomFilepath;

        Stream? _savedRomStream;

        // friendly rom uri selection status
        // to be replaced with URI filename once I manage to get that working
        string _savedRomStatus;

        public string RomFilepath
        {
            get => Path.GetFileName(_romFilepath);
            set
            {
                _romFilepath = value;
                OnPropertyChanged("RomFilepath");
            }
        }

        public string CmtpFilepath
        {
            get => Path.GetFileName(_cmtpFilepath);
            set
            {
                _cmtpFilepath = value;
                OnPropertyChanged("CmtpFilepath");
            }
        }

        // Might go unused, but first I need to figure out how to get the filename from an URI
        public string SavedRomFilepath
        {
            get => _savedRomFilepath;
            set
            {
                _savedRomFilepath = value;
                OnPropertyChanged("SavedRomFilename");
            }
        }

        public string SavedRomStatus
        {
            get
            {
                return _savedRomStream == null ? "No name chosen." : SavedRomFilepath;
            }
            private set { }
        }

        public Stream SavedRomStream
        {
            get => _savedRomStream;
            set
            {
                _savedRomStream = value;
                OnPropertyChanged("SavedRomStream");
            }
        }

        public PatchROMViewModel()
        {
            ChooseROMCommand = new Command(async () => await ChooseROM());
            ChooseCMTPCommand = new Command(async () => await ChooseCMTP());
            ChooseDestCommand = new Command(async () => await ChooseDest());
            StartPatchCommand = new Command(async () => await StartPatch());

            // Subscribe to receive the data from when the user finishes choosing the file of their choice
            MessagingCenter.Subscribe<string, (Stream, string)>("save", "CMTP_FILE_SAVE", (sender, arg) =>
            {
                SavedRomStream?.Close();
                SavedRomStream?.Dispose();
                SavedRomStream = arg.Item1;
                SavedRomFilepath = arg.Item2;
                OnPropertyChanged("SavedRomStatus");
            });
        }

        public async Task ChooseROM()
        {
            // note to self: these try catches are here only because it's 11pm and i don't wanna bother too much with this
            // right now. i have things to work on tomorrow and i am about to sleep
            // IDEALLY the right way to do this is to check if the app has the media reading permission and then request for it
            // before doing anything with the external storage file picker.
            // the file picker will fail with a permission exception and that's enough for me.
            // i don't have an android 13 device to try right now and emulators won't work on my machine currently so i can't
            // test whether the file picker no longer automatically brings up the permission modal and i'm not currently gonna
            // bother anybody.
            // before i go harder on cmtp for android i need to complete cmtp v1.0 which is still in early dev stages
            // so yadda yadda justification for not doing the right thing I WANT TO LIVE

            // i'm so sorry
            try
            {
                await Permissions.RequestAsync<Permissions.StorageRead>();
                FileResult _fr = await FilePicker.PickAsync();
                if (_fr != null)
                {
                    RomFilepath = _fr.FullPath;
                }
            }
            catch (PermissionException)
            {
                Application.Current.MainPage.DisplayAlert(
                    "App has no permission",
                    "Could not open the file picker.\n" +
                          "== IT IS VERY POSSIBLE YOUR DEVICE OR YOU DENIED THE CMTP PATCHER APP THE ABILITY TO OPEN FILES ==\n\n" +
                          "== YOU CAN FIX THIS ==\n\n" +
                          "GO TO APPLICATION SETTINGS AND ALLOW THE APP TO ACCESS MEDIA THEN TRY AGAIN", "OK");
            }

        }

        public async Task ChooseDest()
        {
            try {
                await Permissions.RequestAsync<Permissions.StorageWrite>();
                Intent d = new Intent(Intent.ActionCreateDocument);
                d.AddCategory(Intent.CategoryOpenable).SetType("file/x-cmtp").PutExtra(Intent.ExtraTitle, "output.z64");
                Platform.CurrentActivity.StartActivityForResult(d, 329);
            }
            catch (PermissionException)
            {
                await Application.Current.MainPage.DisplayAlert("App has no permission", "Could not open the file picker.\n\n" +
                    "== IT IS VERY POSSIBLE YOUR DEVICE OR YOU DENIED THE CMTP PATCHER APP THE ABILITY TO OPEN FILES ==\n\n" +
                    "== YOU CAN FIX THIS ==\n\n" +
                    "GO TO APPLICATION SETTINGS AND ALLOW THE APP TO ACCESS MEDIA THEN TRY AGAIN", "OK");
            }
        }

        public async Task ChooseCMTP()
        {
            try
            {
                await Permissions.RequestAsync<Permissions.StorageRead>();
                FileResult _fr = await FilePicker.PickAsync();
                if (_fr != null)
                {
                    CmtpFilepath = _fr.FullPath;
                }
            }
            catch (PermissionException)
            {
                Application.Current.MainPage.DisplayAlert("App has no permission", "Could not open the file picker.\n\n" +
                    "== IT IS VERY POSSIBLE YOUR DEVICE OR YOU DENIED THE CMTP PATCHER APP THE ABILITY TO OPEN FILES ==\n\n" +
                    "== YOU CAN FIX THIS ==\n\n" +
                    "GO TO APPLICATION SETTINGS AND ALLOW THE APP TO ACCESS MEDIA THEN TRY AGAIN", "OK");
            }
        }

        public async Task StartPatch()
        {
            // All guard clauses for safety checks
            if (string.IsNullOrEmpty(_romFilepath))
            {
                await Application.Current.MainPage.DisplayAlert("", $"ROM path cannot be empty", "OK");
                return;
            }

            if (string.IsNullOrEmpty(_cmtpFilepath))
            {
                await Application.Current.MainPage.DisplayAlert("", $"CMTP Patch path cannot be empty", "OK");
                return;
            }

            if (SavedRomStream == null)
            {
                await Application.Current.MainPage.DisplayAlert("", $"You have to save the new ROM somewhere!\n\nSelect the ROM save location.", "OK");
                return;
            }

            FileInfo _fiROM = new FileInfo(_romFilepath);

            if (!File.Exists(_romFilepath))
            {
                await Application.Current.MainPage.DisplayAlert("ROM file missing", $"Make sure the ROM file still exists and choose it again.", "OK");
                return;
            }

            if (!File.Exists(_cmtpFilepath))
            {
                await Application.Current.MainPage.DisplayAlert("CMTP file missing", $"Make sure the CMTP patch still exists and choose it again.", "OK");
                return;
            }

            if (!Patch.CheckROMBigEndian(_romFilepath))
            {
                await Application.Current.MainPage.DisplayAlert("Invalid ROM", $"The provided ROM file is not a valid Big Endian SM64 ROM.\n\nDecomp ROMs are not supported, and your ROM file has to be Z64. Make sure you also didn't choose a compressed file.", "OK");
                return;
            }

            try
            {
                MemoryStream _msCmtp = new MemoryStream(await File.ReadAllBytesAsync(_cmtpFilepath));
                Patch.ReadPatchFile(_msCmtp);
                await _msCmtp.DisposeAsync();
            }
            catch
            {
                await Application.Current.MainPage.DisplayAlert("Invalid CMTP file", $"This doesn't appear to be a CMTP file.\n\nMake sure you are using a .cmtp file previously created by somebody with the CometCHAR Patch Suite in desktop.", "OK");
                return;
            }

            // End of guard clauses

            IsPatching = true;
            StatusText = "Checking ROM size";
            MemoryStream _outLoad;

            if ((_fiROM.Length / 1000000) <= 8)
            {
                await Application.Current.MainPage.DisplayAlert("Small ROM detected!", $"An 8MB ROM! We will now patch this ROM and make it 64MB for the output only.\n\nYour original ROM will remain untouched. This may take a while.", "OK");
                try
                {
                    StatusText = "Patching 8MB ROM\nMay freeze!";
                    // the XDelta patch
                    MemoryStream _deltaLoad = new MemoryStream(Properties.Resources.compatible);
                    MemoryStream _romLoad = new MemoryStream(await File.ReadAllBytesAsync(_romFilepath));
                    _outLoad = new MemoryStream();
                    VCDiff.Decoders.VcDecoder _xdecoder = new VCDiff.Decoders.VcDecoder(_romLoad, _deltaLoad, _outLoad);
                    await _xdecoder.DecodeAsync();

                    // TODO: Move XDelta decoding to base library and include the Experimental Expansion option in there too
                    // so Windows GUI can use it
                    StatusText = "Expanding to 64MB";
                    if ((_outLoad.Length / 1000000) < 64)
                    {
                        _outLoad.SetLength(0x3FFFFFF);
                    }
                    // Free mem
                    await _deltaLoad.DisposeAsync();
                    await _romLoad.DisposeAsync();

                    // For some odd reason I yet cannot understand, perhaps memory management, doing all necessary transformations on memory only
                    // doesn't work, the ROM offsets are borked.
                    // Writing to file once and re-reading as file works, though. That's odd.

                    /*
                    await File.WriteAllBytesAsync(Path.Combine(_exstor.GetPath(), "dummy.z64"), _outLoad.ToArray());
                    _outLoad = new MemoryStream(await File.ReadAllBytesAsync(Path.Combine(_exstor.GetPath(), "dummy.z64")));
                    File.Delete(Path.Combine(_exstor.GetPath(), "dummy.z64"));*/
                    _outLoad.Position = 0;
                }
                catch
                {
                    // If at any point this doesn't work
                    await Application.Current.MainPage.DisplayAlert("Patching failed", $"Oops! It seems as if patching failed.\n\nKeep in mind this patching will not work with Decomp ROMs and you need an extra 64MB of internal storage. If you believe this is an error, please report this bug.", "OK");
                    StatusText = "Ready, but patching failed.";
                    return;
                }
            }
            else
            {
                // Just take the original ROM you were given with no expansion and load itin
                _outLoad = new MemoryStream(await File.ReadAllBytesAsync(_romFilepath));
            }

            // If 24, 48MB ROM
            if ((_outLoad.Length / 1000000) > 8 && (_outLoad.Length / 1000000) < 64)
            {
                await Application.Current.MainPage.DisplayAlert("Unsupported ROM", $"The provided ROM appears to be an old hack. Patching to these is not currently supported. Stay tuned!", "OK");
                return;
            }

            //La Gran Final
            StatusText = "Applying CMTP patch";

            MemoryStream _cmtpMem = new MemoryStream(await File.ReadAllBytesAsync(_cmtpFilepath));
            byte[] resultBytes = await Task.Run(() => Patch.PatchROM(_outLoad, _cmtpMem, null));
            await SavedRomStream.WriteAsync(resultBytes);

            StatusText = "Ready, just finished patching";
            IsPatching = false;
            // Free mem
            _outLoad.Close();
            _cmtpMem.Close();
            SavedRomStream.Close();

            await _cmtpMem.DisposeAsync();
            await SavedRomStream.DisposeAsync();
            await _outLoad.DisposeAsync();
            await Application.Current.MainPage.DisplayAlert("", $"Done!", "OK");
        }

        public ICommand ChooseROMCommand { get; }

        public ICommand ChooseCMTPCommand { get; }

        public ICommand StartPatchCommand { get; }

        public ICommand ChooseDestCommand { get; }

    }
}