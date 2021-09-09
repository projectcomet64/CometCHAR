using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace CometChar.Mobile.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AboutPage : ContentPage
    {
        public AboutPage()
        {
            InitializeComponent();
        }

        private void Button_Clicked(object sender, EventArgs e)
        {
            UpdateCurDirAsync("/");
        }

        public async Task UpdateCurDirAsync(string path)
        {
            MessagingCenter.Subscribe<string, string>("SaveAsModal", "Path", (sender, arg) =>
            {
                if (arg != null)
                {
                    lbCurDir.Text = arg;
                }
                Navigation.PopModalAsync();
                MessagingCenter.Unsubscribe<string, string>("SaveAsModal", "Path");
            }                               
            );
            SaveAsPage modal = new SaveAsPage();
            modal.Disappearing += (sender2, e2) =>
            {
               
            };
            await Navigation.PushModalAsync(modal);
            
        }
    }
}