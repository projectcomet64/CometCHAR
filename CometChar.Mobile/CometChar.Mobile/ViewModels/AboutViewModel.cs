using System;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace CometChar.Mobile.ViewModels
{
    public class AboutViewModel : BaseViewModel
    {
        public AboutViewModel()
        {
            Title = "About";
            OpenGHCommand = new Command(async () => await Browser.OpenAsync("https://github.com/projectcomet64/CometCHAR"));
            OpenDonateCommand = new Command(async () => await Browser.OpenAsync("https://ko-fi.com/glitchypsi"));
            OpenMoreCommand = new Command(async () => await Browser.OpenAsync("https://comet.glitchypsi.xyz"));
            OpenPatreonCommand = new Command(async () => await Browser.OpenAsync("https://patreon.com/GlitchyPSI"));
            OpenGPSICommand = new Command(async () => await Browser.OpenAsync("https://glitchypsi.xyz"));
        }

        public ICommand OpenGHCommand { get; }
        public ICommand OpenDonateCommand { get; }

        public ICommand OpenMoreCommand { get; }
        public ICommand OpenPatreonCommand { get; }
        public ICommand OpenGPSICommand { get; }
    }
}