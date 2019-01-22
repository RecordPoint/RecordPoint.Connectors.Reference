using RecordPoint.Connectors.SDK.Client;
using System.Security;

namespace ReferenceConnectorWorkerService
{
    public static class ConnectorApiAuthHelper
    {
        public static ApiClientFactorySettings GetApiClientFactorySettings()
        {
            return new ApiClientFactorySettings
            {
                ConnectorApiUrl = "https://connector-uat.eigerio.space/connector/" // TODO: add endpoint address here
            };
        }

        public static AuthenticationHelperSettings GetAuthenticationHelperSettings(string tenantDomainName)
        {
            var secureString = new SecureString();
            var clearSecret = "bvriZYbkSDFgK8hOYokuynWyK8l8vA+iCV+JwRmd4nE=";   // TODO: add client secret here
            foreach (var c in clearSecret)
            {
                secureString.AppendChar(c);
            }

            return new AuthenticationHelperSettings
            {
                AuthenticationResource = "https://olympusmons.onmicrosoft.com/ReferenceConnector", // TODO: add authentication resource here
                ClientId = "59c8ca1e-daea-4c15-816a-6a07ed4e27fc", // TODO: add clientid here
                ClientSecret = secureString,
                TenantDomainName = tenantDomainName
            };
        }
    }
}
