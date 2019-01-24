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
                ConnectorApiUrl = "" // TODO: add endpoint address here
            };
        }

        public static AuthenticationHelperSettings GetAuthenticationHelperSettings(string tenantDomainName)
        {
            var secureString = new SecureString();
            var clearSecret = "";   // TODO: add client secret here
            foreach (var c in clearSecret)
            {
                secureString.AppendChar(c);
            }

            return new AuthenticationHelperSettings
            {
                AuthenticationResource = "", // TODO: add authentication resource here
                ClientId = "", // TODO: add clientid here
                ClientSecret = secureString,
                TenantDomainName = tenantDomainName
            };
        }
    }
}
