using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.OS;
using Android.Content;
using System.IO;
using System.Text;
using Microsoft.Maui.Controls.Compatibility;
using Microsoft.Maui.Controls;
using Microsoft.Maui;

namespace CometChar.Mobile
{
    [Activity(Label = "CMTP Patcher", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize)]
    public class MainActivity : MauiAppCompatActivity
    {
        public static MainActivity Instance { get; private set; }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Instance = this;
            Platform.Init(this, savedInstanceState);
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            switch (requestCode)
            {
                case 329:
                    {
                        if (data == null) break;

                        FileInfo _fi = new FileInfo(data.DataString);
                        string delta = Android.Net.Uri.Parse(data.DataString).Path;
                        Stream stream = Platform.AppContext.ContentResolver.OpenOutputStream(Android.Net.Uri.Parse(data.DataString));
                        MessagingCenter.Send("save", "CMTP_FILE_SAVE", (stream, _fi.Name));
                        break;
                    }
            }

        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}