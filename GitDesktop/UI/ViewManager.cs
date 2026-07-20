using System;
using System.Collections.Generic;

namespace GitDesktop.UI
{
    internal static class ViewManager
    {
        public static Action<IRender>? AddNewView { get; set; }
        public static Action<IRender>? RemoveView { get; set; }
    }
}
