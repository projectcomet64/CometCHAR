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
        public void SaveAs(string fname)
        {
            Activity context = Xamarin.Essentials.Platform.CurrentActivity;
            Intent d = new Intent(Intent.ActionCreateDocument);
            d.AddCategory(Intent.CategoryOpenable).SetType("file/x-cmtp").PutExtra(Intent.ExtraTitle, fname);
            context.StartActivityForResult(d, 329);
        }

        public bool CanReadExternal()
        {
            return Environment.MediaMountedReadOnly.Equals(Environment.ExternalStorageState);
        }

        public bool CanWriteExternal()
        {
            return Environment.MediaMounted.Equals(Environment.ExternalStorageState);
        }

        public string GetPath()
        {
            Context context = Android.App.Application.Context;
            var filePath = context.GetExternalFilesDir("");
            return filePath.Path;
        }
    }
}
