# sftp-watcher

Monitors the contents of given SFTP folders and emits Azure Storage Queue/Service Bus events for files being created/removed/modified.
Implemented with Azure Functions Durable Entities. Uses [SSH.NET](https://github.com/sshnet/SSH.NET)'s **SftpClient** for communication.

## Config Settings

* (required) **FOLDERS_TO_WATCH** - JSON string of the following format: 
    ```
    {
	    'my-server.com/myfolder/*.*': {'my-user': 'my-password'}, 
	    'my-other-server.org/*.xml': {'my-other-user': 'password-to-my-other-server'}
	    ...
    }
    ```
    Folder URL may or may not contain a subpath, but it is required to contain server name and file mask (at the end). All subfolders within the given path are recursively traversed.
    
    Password may be a plain string password or (preferrably) an Azure Key Vault secret reference (e.g. `https://my-vault.vault.azure.net/secrets/my-sftp-password/123456789`). In the latter case you'll need to configure a Managed Identity for your Function App instance.
    
    Per each individual folder an instance of Azure Functions Durable Entity is created, which stores the current folder structure in its state and periodically tries to detect changes. Once created, you can monitor those Durable Entities with [Durable Functions Monitor](https://github.com/scale-tone/DurableFunctionsMonitor).

* (required) **POLLING_INTERVAL_CRON_EXP** - CRON expression, that defines the polling period. E.g. `*/5 * * * * *`.
* (required) **OUTPUT_QUEUE_OR_TOPIC_NAME** - queue or topic name to output messages to. When publishing events to Event Grid, this value should contain full custom topic URL, e.g. `https://my-event-grid-custom-topic.northeurope-1.eventgrid.azure.net/api/events`.
* (optional) **SERVICE_BUS_CONN_STRING** - Azure Service Bus connection string. 
	If specified, messages will be sent to a queue/topic in that Service Bus namespace. `Message.Label` will be set to the string representation of [WhatHappenedEnum](https://github.com/scale-tone/sftp-watcher/blob/main/SftpWatcherEntity.cs#L175) (so that you could potentially use [Service Bus topic filters](https://docs.microsoft.com/en-us/azure/service-bus-messaging/topic-filters)), message body will contain full path to the changed file.
	
* (optional) **EVENT_GRID_TOPIC_KEY** - Azure Event Grid custom topic key.
 	If specified, messages will be sent to an Event Grid custom topic, that you specify via **OUTPUT_QUEUE_OR_TOPIC_NAME** (that setting then should contain full custom topic URL).

	If both **SERVICE_BUS_CONN_STRING** and **EVENT_GRID_TOPIC_KEY** are omitted, messages will be sent to a Storage queue in the underlying Storage account. Message body will contain JSON representation of [StorageQueueMessage](https://github.com/scale-tone/sftp-watcher/blob/main/SftpToStorageQueueWatcherEntity.cs#L30).
	
* (optional) **STAY_SILENT_AT_FIRST_RUN** - set it to `true`, if you don't want `FileAdded` events to be emitted for every existing file at first run.
* (optional) **SFTP_TIMEOUT_IN_SECONDS** - timeout for all SFTP operations, in seconds. Default value is 5 seconds.

## How to deploy to Azure

[![Deploy to Azure](https://aka.ms/deploytoazurebutton)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2Fscale-tone%2Fsftp-watcher%2Fmain%2Farm-template.json)

The above button will deploy these sources to a newly created Azure Functions instance (with Premium plan).
