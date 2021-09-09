using System;
using System.Collections.Generic;
using System.Text;

namespace CometChar.Mobile.Services
{
    public interface IExternalStorage
    {
        string GetPath();
        bool CanReadExternal();
        bool CanWriteExternal();
    }
}
