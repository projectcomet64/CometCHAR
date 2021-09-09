using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace CometChar.Mobile.ViewModels
{
    public class PatchInfoViewModel : BaseViewModel
    {
        private Structs.PatchInformation _patchInfo;
        public Structs.PatchInformation PatchInfo { get => _patchInfo; set => _patchInfo = value; }

        public bool _hasError;

        public PatchInfoViewModel()
        {
            OpenFileCmd = new Command(async () => { await PickPatchFile(); });
            OnPropertyChanged("PatchInfoVersion");
            OnPropertyChanged("PatchVersionDescription");
        }

        public string PatchInfoVersion
        {
            get
            {
                if ((_patchInfo.versionMajor == 0 && _patchInfo.versionMinor == 0) || _hasError)
                {
                    return "N/A";
                }

                return $"v{_patchInfo.versionMajor}.{_patchInfo.versionMinor}";
            }
        }

        public string PatchVersionDescription
        {
            get
            {

                if (_hasError)
                {
                    return "This doesn't appear to be a CometCHAR patch?";
                }

                if (_patchInfo.versionMajor == 0 && _patchInfo.versionMinor == 0)
                {
                    return "Open a patch file to see its info.";
                }

                int isExt = _patchInfo.Features & 1;
                return isExt != 0 ? "This patch appears to be from a Blender / Fast64 / Extended Classic mod." : "This patch appears to be from a Classic mod.";
            }
        }

        public async Task PickPatchFile()
        {
            _hasError = false;
            try
            {
                FileResult fr = await FilePicker.PickAsync(new PickOptions { PickerTitle = "Select a CMTP patch" });
                if (fr == null) return;

                _patchInfo = Patch.ReadPatchFile(await fr.OpenReadAsync());
                OnPropertyChanged("PatchInfoVersion");
                OnPropertyChanged("PatchVersionDescription");
            }
            catch (Exception ex)
            {
                _hasError = true;
                OnPropertyChanged("PatchInfoVersion");
                OnPropertyChanged("PatchVersionDescription");
#if DEBUG
                Debug.WriteLine(ex.Message);
#endif
            }

        }

        public ICommand OpenFileCmd { get; }
    }
}
