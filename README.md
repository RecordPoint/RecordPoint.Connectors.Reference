# Introduction 
This project is a template for creating a Connector for Recordpoint using the Connector SDK ([GitHub link][github-link]). 

The README for the Connector SDK covers essential concepts needed to build a connector. It is required reading and can be accessed via the provided SDK links. Topics include an overview of the RecordPoint platform, the workings of the Connector SDK, and the objectives of a connector connecting to the platform.

## Caveat

As this is a "demo" connector, there are some compromises in the design, or places where things would be done differently in a real connector. 
These are called out with code comments.
(For example, the Reference Connector is unable to detect renames and deletions for file and directories. Most other connectors should try to detect these.)

Most connectors are cloud-based. This repo does not include infrastructure and CICD code required to build a cloud connector. Refer to the Cloud Reference Connector for a template for that part of the code. (The CRC is not hosted on GitHub.)

# Installation guide

## Prerequisites
- Chocolatey
- .NET 8 SDK
- Eiger (RecordPoint) environment to connect to (local or SINT)

## Process 

1. Open Powershell as administrator to this dir.
2. Run `Setup.ps1`.

> After running the setup script, RabbitMq will always run at computer startup.
> 
> See [here][rabbitmq-service] for how to disable this behavior. If you do this, you will have to start RabbitMq before running the Reference Connector.
>
> You could also uninstall the RabbitMq when you're done with the Reference Connector. You can run Setup.ps1 again if you need to reinstall.

3. Open appsettings.json in a text editor.
4. Open [http://localhost:15672/][rabbitmq-localhost] in the browser
   - Login with username 'admin' and password 'admin'
   - Find the cluster name in the top right - e.g. "rabbit@WIN-2034939"
   - Copy the bit after the @ - e.g. "WIN-2034939"
   - Use for this appsettings property: RabbitMqOptions.HostName
5. Get the Entra ID settings. 
   - If you want to connect to a local Eiger environment, go to the 'Creating an Entra ID app' section below.
   - If you want to connect to SINT, go to 'Getting access to the SINT Entra ID application' below.
6. Open Visual Studio.
   - Enable Multi Project Launch Configuation https://devblogs.microsoft.com/visualstudio/multi-project-launch-configuration/
   - Run the connector using the "All Services" build profile.
   - *(This is required to set up RabbitMq queues.)*
7. Create one or more connector configs in Records365. 
   - See 'Creating a config' section below.
8. Create a directory to contain test files that will be synced to RecordPoint.
   - Copy the full path into this appsettings property: ConnectorConfigs -> Directory
   - Create a subdirectory. *(This will become a 'Channel'.)*
   - Create a subdirectory within that subdirectory. *(This will become an 'Aggregation'.)*
   - Create one or more text files within this subdirectory. *(This will become a 'Record'.)*
   - *(You may create as many Channels, Aggregations and Records as you like. Records must have a parent Aggregation, and Aggregations a parent Channel, or they won't be picked up by the connector.)*
8. Open Visual Studio.
   - Enable Multi Project Launch Configuration [here][multi-project-launch]
   - Run the connector using the "All Services" build profile.
   - *(This is required to set up RabbitMq queues.)*

The app is now ready for use. 
You may want to read "Customizing the behavior of the SDK" below.

### Creating an Entra ID application 
This is only required for local environments. 

1. Go to Azure Portal -> Entra ID -> App Registrations
2. Create a new App Registration 
   - Use "Accounts in any organizational directory (Any Entra ID - Multitenant)" setting 
   - Use "Web" redirect URI with value "https://localhost/administration/connectors/callback"
3. Authentication tab -> Tick "Access tokens"
4. Certificates & Secrets tab -> Create secret with expiry far in the future
5. Copy client secret value
6. Copy client ID (main page for app registration)
7. Edit appsettings:
   - Set Configuration.ClientId and Configuration.ClientSecret based on Entra ID app details
   - Set Configuration.Audience to "https://rpfabricdev.onmicrosoft.com/rpfabric"
   - Set Configuration.ConnectorApiUrl to "https://localhost:44366/connector/"

### Creating a connector

A connector (or connector config) is a list of settings (which a customer supplies in Records365) to link an account in the content source to the connector type.

The Reference Connector can (and all connector types should be able to) monitor more than one connector config.

1. Open Records365.
2. Go to the cog in the top right -> Add connector -> "..." in the top right -> "New Connector Type"
3. Set Client ID to the Entra ID app client ID.
4. Change Notification Method to "Pull".
5. Fill in the rest of the fields. Any values are fine. (Do not change Notification Types or Allow Client ID Override.)
6. Save. 
7. Add connector -> select the type you just made -> Fill in the fields however you like
8. Enable and save.
9. Click on the connector and download the settings.
10. Open the settings file.
11. Copy the contents into a new node in appsettings -> ConnectorConfigs. Here's an example with multiple connector configs:
```
"ConnectorConfigs": [
   {
      "Directory": "C:\\Users\\MyUsername\\Documents\\Test Files\\ReferenceConnector\\Tenant2",
      "ConnectorId": "d40eaf72-7200-441c-a1c5-8d55539a39d9",
      "ConnectorTypeId": "edcf148b-44e5-405e-be2d-da4154182b8f",
      "TenantId": "689049ca-b5cb-45db-87d8-7f54852570d1",
      "TenantDomainName": "rptenant2.onmicrosoft.com",
      "ConnectorApiUrl": "https://localhost:44366/connector/",
      "ClientId": "1cd68a5a-d4a9-4afb-99c8-7d817d1ae57c",
      "Audience": "https://rpfabricdev.onmicrosoft.com/rpfabric"
   },
   {
      "Directory": "C:\\Users\\MyUsername\\Documents\\Test Files\\ReferenceConnector\\Tenant1",
      "ConnectorId": "62f4df1d-e2fd-49b5-9bcd-dc867dad7f84",
      "TenantId": "a7adb3ae-6a58-4731-b628-5743f6244a28",
      "TenantDomainName": "rptenant1.onmicrosoft.com",
      "ConnectorApiUrl": "https://localhost:44366/connector/",
      "ClientId": "1cd68a5a-d4a9-4afb-99c8-7d817d1ae57c",
      "Audience": "https://rpfabricdev.onmicrosoft.com/rpfabric",
      "DefaultIngestionMode": "Selected",
      "Included": [ "Channel2" ],
      "ContentRegistrationMode":  "All"
   }
],
```

To create another config in the same R365 tenant, repeat steps 7-11.
To create another config in a different R365 tenant, repeat all steps.

[github-link]: https://github.com/RecordPoint/RecordPoint.Connectors.SDK
[rabbitmq-service]: https://www.rabbitmq.com/docs/man/rabbitmq-service.8
[rabbitmq-localhost]: http://localhost:15672/
[multi-project-launch]: https://devblogs.microsoft.com/visualstudio/multi-project-launch-configuration/

### Customising the behaviour of the SDK

The default appsettings for this project includes settings to control the behaviour of the SDK (e.g. how often operations run).

(Outside of local development, these are usually not specified.)

Refer to [ContentManagerOptions](https://github.com/RecordPoint/RecordPoint.Connectors.SDK/blob/master/RecordPoint.Connectors.SDK.Abstractions/ContentManager/ContentManagerOptions.cs) (and other classes that use it, e.g. ChannelDiscoveryOptions).

### Resetting state

Run ClearLocalState.ps1 to empty the RabbitMQ queues and database. 

Deleting records from Records365 is not recommended. Ask the team for help if you need to do it. 