using System;
using System.Collections.Generic;
using System.Text;

namespace CometChar.Mobile.Services
{
    public interface IExternalStorage
    {
        string GetPath();
        void SaveAs(string fname);
        bool CanReadExternal();
        bool CanWriteExternal();
    }
}
