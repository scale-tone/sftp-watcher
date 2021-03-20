using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Newtonsoft.Json.Linq;

namespace SftpWatcher
{
    // Pings all watcher entities periodically
    public static class TimerTrigger
    {
        [FunctionName(nameof(TimerTrigger))]
        public static async Task Run([TimerTrigger("%POLLING_INTERVAL_CRON_EXP%")] TimerInfo myTimer, [DurableClient] IDurableEntityClient durableClient)
        {
            dynamic foldersToWatch = JObject.Parse(Environment.GetEnvironmentVariable("FOLDERS_TO_WATCH"));

            foreach (var folder in foldersToWatch)
            {
                string folderFullPath = folder.Name;

                // Using Storage Queue or Service Bus watcher, depending on the config
                string entityName = string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SERVICE_BUS_CONN_STRING")) ?
                    nameof(SftpToStorageQueueWatcherEntity) : nameof(SftpToServiceBusQueueWatcherEntity);

                // Deriving entityKey from folder name
                string key = folderFullPath.Replace("/", "-").Replace("\\", "-").Replace("#", "-").Replace("?", "-");

                await durableClient.SignalEntityAsync<ISftpWatcherEntity>(new EntityId(entityName, key), e => e.Watch(folderFullPath));
            }
        }
    }
}
