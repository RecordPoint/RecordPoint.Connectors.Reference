# ReferenceConnectorSF
A reference implementation of a multi-tenanted cloud connector using .NET Core and Azure Service Fabric.

Requires Visual Studio 15.8.1 or later.

# Deployment QuickStart

This QuickStart guide will show you how to deploy ReferenceConnectorSF to a local Service Fabric cluster and set it up to submit content to a Records365 tenant.

## Create an Azure AD Application

In the [Azure Portal](https://portal.azure.com), create a new App Registration in the Azure Active Directory that is linked to your Records365 vNext tenant. 
For example, if you log in to your Records365 vNext tenant as `john.doe@mytenant.onmicrosoft.com`, then create 
a new App Registration in the `mytenant.onmicrosoft.com` directory. Provide the following details when creating the App Registration:

*  **Name**: Anything you like
*  **Application type**: Web app/API
*  **Sign on URL**: Any valid URL - it doesn't really matter, this value isn't used anywhere

Once the App Registration is created, take note of the value for `Application ID` and then navigate to `Settings`.

Navigate to `Properties` and take note of the `App ID URI` value.

Finally, under `Keys`, create a new Key and take note of its value. 

## Update Settings in ReferenceConnectorSF

In the ReferenceConnectorSF solution, open `ConnectorApiAuthHelper.cs` and plug in the appropriate values from the Azure Active Directory App Registration created above. Ensure the following values are updated:
*  **Client ID**: this is the `Application ID` of the App
*  **Client Secret**: this is the `Key` 
*  **Authentication Resource**: the `App ID URI` of the App
*  **Connector API URL**: the Connectors API from of your Records365 environment. For a local environment, this is `https://localhost:44366/`

## Deploy ReferenceConnectorSF

Clone the repository, open in Visual Studio, build and deploy to your local Service Fabric cluster. The application should be "green" in Service Fabric Explorer.

## Install ngrok 

The Records365 Connector Framework uses webhooks to send notifications to the connector. Records365 can not send webhook calls directly into a local development environment; we need to use a tunnel. ngrok provides this tunnel. 

Sign up and download ngrok to your local environment from [here](https://ngrok.com/). The free plan is sufficient for our purposes. Ensure the auth token setup step is run. 

Run ngrok with the following command line:

    ngrok http 8555 

This sets up a tunnel to local port 8555; this is the port that the ReferenceConnectorSF webhook listener API is listening on.

When ngrok starts, it will output a publicly accessible HTTPS URL that ngrok is listening on. Take note of this URL.

Note that if ngrok is restarted, a new URL will be generated.

## Register a Connector Type in Records365

In Records365, log in as an Application Administrator. Click the settings cog, then click "Connectors" on the left navigation menu. 
Click "Add Connector", then click the ellipsis menu in the top right and select "New Connector Type". Provide the following details:

*  **Name**: Name of your connector type.
*  **Short Name**: Short name of your connector type.
*  **Content Source**: Name of the content source that your connector adapts to. 
*  **Publisher**: Name of the organization that publishes your connector type.
*  **Custom Settings Page URL**: Leave this blank.
*  **Client ID**: The `Application ID` from the Azure Active Directory App Registration created above.
*  **Allow Client ID Override**: No.
*  **Notification Method**: Push.
*  **Notification Authentication Resource**: The `App ID URI` of the Azure Active Directory App Registration created above.
*  **Notification URL**: The ngrok URL given above, with "/api/" appended to it. For example, if ngrok is listening on https://abcdefg.ngrok.io, the Notification URL is https://abcdefg.ngrok.io/api/. Note that if ngrok is restarted, this value will need to be updated with the new URL.
*  **Notification Types**: Check "Item Destroyed", "Connector Created", "Connector Updated", "Connector Deleted", and leave others blank.
*  **Logo**: Upload a 360px by 160px image.
*  **Icon**: Upload a 70px by 70px image.

Click Save. Your custom connector type will now appear in the Connectors Gallery.

## Create a Connector 

Click the Add button on your new custom connector type. A new instance of the connector will be created. This should have sent a webhook notification to ReferenceConnectorSF in your local environment via ngrok. This can be verified in the ngrok window - there should be a line at the bottom of the output that reads 

    POST /api/notifications      200 OK

Next, enable the connector in Records365. This will send another webhook notification to ReferenceConnectorSF, so we should see another line in the ngrok output after enabling the connector.

At this point ReferenceConnectorSF should be submitting records, aggregations, audit events and binaries to your tenant. Content is submitted once per minute.

## Debug ReferenceConnectorSF

Attach the Visual Studio debugger to the ReferenceConnectorSF processes:

*  ReferenceConnectorService.exe
*  ReferenceconnectorWorkerService.exe

Once attached, set breakpoints and step through the code. 

