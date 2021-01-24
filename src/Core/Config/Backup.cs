using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace M365.TeamsBackup.Core.Config
{
    public class Backup
    {
        public string Path { get; set; }
        public bool ShouldZip { get; set; }
        public string TeamId { get; set; }
        public bool JsonWriteIndented { get; set; }

        private JsonSerializerOptions _JsonOptions;
        
        [JsonIgnoreAttribute]
        public JsonSerializerOptions JsonOptions
        {
            get
            {
                if (_JsonOptions == null)
                {
                    _JsonOptions = new JsonSerializerOptions()
                    {
                        WriteIndented = JsonWriteIndented
                    };
                }
                return _JsonOptions;
            }
        }
    }
}
