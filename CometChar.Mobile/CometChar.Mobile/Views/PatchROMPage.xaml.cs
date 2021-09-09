using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace CometChar.Mobile.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class PatchROMPage : ContentPage
    {
        public PatchROMPage()
        {
            InitializeComponent();
        }

        private void btnChooseOutput_Clicked(object sender, EventArgs e)
        {
            UpdateSavePathAsync("/");
        }

        public async Task UpdateSavePathAsync(string path)
        {
            MessagingCenter.Subscribe<string, string>("SaveAsModal", "Path", (sender, arg) =>
            {
                if (arg != null)
                {
                    tbRomSavePath.Text = arg;
                }
                Navigation.PopModalAsync();
                MessagingCenter.Unsubscribe<string, string>("SaveAsModal", "Path");
            }
            );
            SaveAsPage modal = new SaveAsPage();
            await Navigation.PushModalAsync(modal);

        }
    }
}