using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace M365.TeamsBackup.Core.Services.Util
{
    public static class MgGraphRequester
    {
        public const int MaxRetry = 5;
        public static async Task<bool> ShouldContinue(ServiceException mgsex, int currentRetry)
        {
            int waitForMilliSeconds;

            switch (mgsex.StatusCode)
            {
                case System.Net.HttpStatusCode.TooManyRequests:
                    waitForMilliSeconds = 1000 * currentRetry * currentRetry;
                    break;
                case System.Net.HttpStatusCode.Unauthorized:
                    waitForMilliSeconds = 1000 * currentRetry * currentRetry;
                    break;
                case System.Net.HttpStatusCode.BadGateway:
                    waitForMilliSeconds = 1000 * currentRetry * currentRetry;
                    break;
                case System.Net.HttpStatusCode.Forbidden:
                    return false;
                case System.Net.HttpStatusCode.NotFound:
                case System.Net.HttpStatusCode.BadRequest:
                default:
                    return false;
            }
            if (currentRetry == MaxRetry){
                return false;
            } else {
                await Task.Delay(waitForMilliSeconds);
            }

            return true;
        }
    }

}
