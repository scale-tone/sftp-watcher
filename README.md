# sftp-watcher

Monitors the contents of given SFTP folders and emits Azure Storage Queue/Service Bus events for files being created/removed/modified.
Implemented with Azure Functions Durable Entities.

# Config Settings

* (required) **FOLDERS_TO_WATCH** - JSON string of the following format: 
    ```
    {
	    'my-server.com/myfolder/*.*': {'my-user': 'my-password'}, 
	    'my-other-server.org/*.xml': {'my-other-user': 'password-to-my-other-server'}
    }
    ```
    Folder URL may or may not contain a subpath, but it is required to contain server name and file mask (at the end). All subfolders within the given path are recursively traversed.
    Password may be a plain string password or (preferrably) an Azure Key Vault secret reference (e.g. `https://konst-vault.vault.azure.net/secrets/my-sftp-password/123456789`). In the latter case you'll need to configure a Managed Identity for your Function App instance.

* (required) **POLLING_INTERVAL_CRON_EXP** - CRON expression, that defines the polling period. E.g. `*/5 * * * * *`.
* (required) **OUTPUT_QUEUE_OR_TOPIC_NAME** - queue or topic name to output messages to.
* (optional) **SERVICE_BUS_CONN_STRING** - Azure Service Bus connection string. If specified, messages will be sent to a queue/topic in that Service Bus namespace. If omitted, messages will be sent to a Storage queue in the underlying Storage account.



