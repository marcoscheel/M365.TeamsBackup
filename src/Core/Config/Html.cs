using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace M365.TeamsBackup.Core.Config
{
    public class Html
    {
        public string SourcePath { get; set; }
        public string TargetPath { get; set; }
        public string DateTimeFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";
        public string TemplateFile { get; set; }
        public bool UseInlineImages { get; set; } = true;
        public bool CreateSingleHtmlForMessage { get; set; } = true;

    }
}
