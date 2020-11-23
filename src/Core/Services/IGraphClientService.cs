using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace M365.TeamsBackup.Core.Services
{
    public interface IGraphClientService
    {
        GraphServiceClient GetGraphClient(ILogger logger);
    }
}
