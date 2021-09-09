using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using CometChar.Mobile.Droid;
using CometChar.Mobile.Services;
using Xamarin.Forms;
using System.Text;

[assembly: Dependency(typeof(ExternalStorageService))]
namespace CometChar.Mobile.Droid
{
    public class ExternalStorageService : IExternalStorage
    {
        public string GetPath()
        {
            Context context = Android.App.Application.Context;
            var filePath = context.GetExternalFilesDir("");
            return filePath.Path;
        }

        public bool CanReadExternal()
        {
            return Environment.MediaMountedReadOnly.Equals(Environment.ExternalStorageState);
        }

        public bool CanWriteExternal()
        {
            return Environment.MediaMounted.Equals(Environment.ExternalStorageState);
        }

    }
}
