using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace M365.TeamsBackup.Core.Config
{
    public static class App
    {
#if DEBUG
        public static readonly string Version = typeof(App).Assembly.GetName().Version.ToString() + "-dbg";
#else
        public static readonly string Version = typeof(App).Assembly.GetName().Version.ToString();
#endif
    }
}
