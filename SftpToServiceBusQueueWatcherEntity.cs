using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.ServiceBus;
using System.Text;

namespace SftpWatcher
{
    // Checks the list of files in an SFTP folder and emits events to Azure Service Bus queue/topic
    public class SftpToServiceBusQueueWatcherEntity: SftpWatcherEntity
    {
        public SftpToServiceBusQueueWatcherEntity(ICollector<Message> serviceBusCollector)
        {
            this.eventCollector = serviceBusCollector;
        }

        protected override void EmitEvent(WhatHappenedEnum eventType, string filePath)
        {
            this.eventCollector.Add(new Message { 
                Label = eventType.ToString("g"),
                Body = Encoding.UTF8.GetBytes(filePath)
            });
        }

        private readonly ICollector<Message> eventCollector;

        // Required boilerplate
        [FunctionName(nameof(SftpToServiceBusQueueWatcherEntity))]
        public static Task Run([EntityTrigger] IDurableEntityContext ctx,
            [ServiceBus("%OUTPUT_QUEUE_OR_TOPIC_NAME%", Connection = "SERVICE_BUS_CONN_STRING")] ICollector<Message> storageQueueCollector
        ) => ctx.DispatchAsync<SftpToServiceBusQueueWatcherEntity>(storageQueueCollector);
    }
}
