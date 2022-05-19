using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Azure.Messaging.EventGrid;

namespace SftpWatcher
{
    // Checks the list of files in an SFTP folder and emits events to Azure Event Grid custom topic
    public class SftpToEventGridTopicWatcherEntity: SftpWatcherEntity
    {
        public SftpToEventGridTopicWatcherEntity(ICollector<EventGridEvent> eventGridCollector)
        {
            this.eventCollector = eventGridCollector;
        }

        protected override void EmitEvent(WhatHappenedEnum eventType, string filePath)
        {
            this.eventCollector.Add(new EventGridEvent(
                filePath,
                eventType.ToString("g"),
                "1.0",
                new {
                    eventType = eventType.ToString("g"),
                    filePath
                }
            ));
        }

        private readonly ICollector<EventGridEvent> eventCollector;

        // Required boilerplate
        [FunctionName(nameof(SftpToEventGridTopicWatcherEntity))]
        public static Task Run([EntityTrigger] IDurableEntityContext ctx,
            [EventGrid(TopicEndpointUri = "EVENT_GRID_TOPIC_URL", TopicKeySetting = "EVENT_GRID_TOPIC_KEY")] ICollector<EventGridEvent> storageQueueCollector
        ) => ctx.DispatchAsync<SftpToEventGridTopicWatcherEntity>(storageQueueCollector);
    }
}
