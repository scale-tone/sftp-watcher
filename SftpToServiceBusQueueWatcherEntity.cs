using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Azure.Messaging.ServiceBus;

namespace SftpWatcher
{
    // Checks the list of files in an SFTP folder and emits events to Azure Service Bus queue/topic
    public class SftpToServiceBusQueueWatcherEntity: SftpWatcherEntity
    {
        public SftpToServiceBusQueueWatcherEntity(ICollector<ServiceBusMessage> serviceBusCollector)
        {
            this.eventCollector = serviceBusCollector;
        }

        protected override void EmitEvent(WhatHappenedEnum eventType, string filePath)
        {
            this.eventCollector.Add(new ServiceBusMessage { 
                Subject = eventType.ToString("g"),
                Body = new BinaryData(filePath)
            });
        }

        private readonly ICollector<ServiceBusMessage> eventCollector;

        // Required boilerplate
        [FunctionName(nameof(SftpToServiceBusQueueWatcherEntity))]
        public static Task Run([EntityTrigger] IDurableEntityContext ctx,
            [ServiceBus("%OUTPUT_QUEUE_OR_TOPIC_NAME%", Connection = "SERVICE_BUS_CONN_STRING")] ICollector<ServiceBusMessage> storageQueueCollector
        ) => ctx.DispatchAsync<SftpToServiceBusQueueWatcherEntity>(storageQueueCollector);
    }
}
