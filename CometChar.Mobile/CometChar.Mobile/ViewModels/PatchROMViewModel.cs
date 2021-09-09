using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;
using VCDiff;
using CometChar;
using System.Diagnostics;

namespace CometChar.Mobile.ViewModels
{
    public class PatchROMViewModel : BaseViewModel
    {

        string _romFilepath;

        string _cmtpFilepath;

        string _savedRomFilepath;

        public string RomFilepath
        {
            get => _romFilepath;
            set
            {
                _romFilepath = value;
                OnPropertyChanged("RomFilepath");
            }
        }

        public string CmtpFilepath
        {
            get => _cmtpFilepath;
            set
            {
                _cmtpFilepath = value;
                OnPropertyChanged("CmtpFilepath");
            }
        }

        public string SavedRomFilepath
        {
            get => _savedRomFilepath;
            set
            {
                _savedRomFilepath = value;
                OnPropertyChanged("SavedRomFilename");
            }
        }

        public PatchROMViewModel()
        {
            ChooseROMCommand = new Command(async () => await ChooseROM());
            ChooseCMTPCommand = new Command(async () => await ChooseCMTP());
            StartPatchCommand = new Command(async () => await StartPatch());
        }

        public async Task ChooseROM()
        {
            FileResult _fr = await FilePicker.PickAsync();
            if (_fr != null)
            {
                RomFilepath = _fr.FullPath;
            }
        }

        public async Task ChooseCMTP()
        {
            FileResult _fr = await FilePicker.PickAsync();
            if (_fr != null)
            {
                CmtpFilepath = _fr.FullPath;
            }
        }

        public async Task StartPatch()
        {
            await Application.Current.MainPage.DisplayAlert("Performing action", $"ROM file: {_romFilepath}\nCMTP file: {_cmtpFilepath}\nSave to: {_savedRomFilepath}\n\nSupposing they exist, patch Now", "OK");
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
                _msCmtp.Dispose();
            }
            catch
            {
                await Application.Current.MainPage.DisplayAlert("Invalid ROM", $"The provided ROM file is not a valid Big Endian SM64 ROM.\n\nDecomp ROMs are not supported, and your ROM file has to be Z64. Make sure you also didn't choose a compressed file.", "OK");
                return;
            }

            if ((_fiROM.Length / 1000000) <= 8)
            {
                await Application.Current.MainPage.DisplayAlert("Small ROM detected!", $"An 8MB ROM! We will now patch this ROM and make it 64MB. We will put it in the same location as this one.\n\nThis may take a while. Also, keep it. You will want to use it for future patching.", "OK");
                try
                {
                    // the XDelta patch
                    MemoryStream _deltaLoad = new MemoryStream(Properties.Resources.compatible);
                    MemoryStream _romLoad = new MemoryStream(await File.ReadAllBytesAsync(_romFilepath));
                    MemoryStream _outLoad = new MemoryStream();
                    VCDiff.Decoders.VcDecoder _xdecoder = new VCDiff.Decoders.VcDecoder(_romLoad, _deltaLoad, _outLoad);
                    await _xdecoder.DecodeAsync();

                    // TODO: Move XDelta decoding to base library and include the Experimental Expansion option in there too
                    // so Windows GUI can use it

                    if ((_outLoad.Length / 1000000) < 64)
                    {
                        _outLoad.SetLength(0x3FFFFFF);
                    }
                    // Patch done, fullpatch, so we will now do the rest
                    File.WriteAllBytes($"{_romFilepath}.xd.z64", _outLoad.ToArray());
                    _romFilepath += ".xd.z64";
                    if (!File.Exists(_romFilepath))
                    {
                        await Application.Current.MainPage.DisplayAlert("Unknown error!", $"Patch failed. New file doesn't exist! This must be a bug. Please report it!", "OK");
                        return;
                    }
                    _fiROM = new FileInfo(_romFilepath);

                    _deltaLoad.Dispose();
                    _romLoad.Dispose();
                    _outLoad.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                    await Application.Current.MainPage.DisplayAlert("Patching failed", $"Oops! It seems as if patching failed.\n\nKeep in mind this patching will not work with Decomp ROMs. If you believe this is an error, please report this bug.", "OK");
                }
            }

            if ((_fiROM.Length / 1000000) > 8 && (_fiROM.Length / 1000000) < 64)
            {
                await Application.Current.MainPage.DisplayAlert("Unsupported ROM", $"The provided ROM appears to be an old hack. Patching to these is not currently supported. Stay tuned!", "OK");
                return;
            }


            MemoryStream _cmtpMem = new MemoryStream(await File.ReadAllBytesAsync(_cmtpFilepath));
            Patch.PatchROM(_romFilepath, _cmtpMem, _savedRomFilepath, null);

            await Application.Current.MainPage.DisplayAlert("", $"Done!", "OK");
        }

        public ICommand ChooseROMCommand { get; }

        public ICommand ChooseCMTPCommand { get; }

        public ICommand StartPatchCommand { get; }

    }
}