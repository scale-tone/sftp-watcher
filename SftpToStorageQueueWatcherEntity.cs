using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace SftpWatcher
{
    // Checks the list of files in an SFTP folder and emits events to Azure Storage queue
    public class SftpToStorageQueueWatcherEntity: SftpWatcherEntity
    {
        public SftpToStorageQueueWatcherEntity(ICollector<StorageQueueMessage> storageQueueCollector)
        {
            this.eventCollector = storageQueueCollector;
        }

        protected override void EmitEvent(WhatHappenedEnum eventType, string filePath)
        {
            this.eventCollector.Add(new StorageQueueMessage(eventType, filePath));
        }

        private readonly ICollector<StorageQueueMessage> eventCollector;

        // Required boilerplate
        [FunctionName(nameof(SftpToStorageQueueWatcherEntity))]
        public static Task Run([EntityTrigger] IDurableEntityContext ctx,
            [Queue("%OUTPUT_QUEUE_OR_TOPIC_NAME%")] ICollector<StorageQueueMessage> storageQueueCollector
        ) => ctx.DispatchAsync<SftpToStorageQueueWatcherEntity>(storageQueueCollector);
    }

    public class StorageQueueMessage
    {
        public StorageQueueMessage(WhatHappenedEnum eventType, string filePath)
        {
            this.EventType = eventType.ToString("g");
            this.FilePath = filePath;
        }

        public string EventType { get; private set; }
        public string FilePath { get; private set; }
    }
}
