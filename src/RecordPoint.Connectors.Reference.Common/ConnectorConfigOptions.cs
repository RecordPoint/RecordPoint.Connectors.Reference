namespace RecordPoint.Connectors.Reference.Common;

/// <summary>
/// Settings for a single connector config.
/// NOT REQUIRED outside the ReferenceConnector.
/// (UNLESS you need to connect a local instance of the connector to SINT.)
/// </summary>
/// <remarks>
/// The Reference Connector uses the Custom Connector UI,
/// which does not have an option for inputting extra settings for connector configs.
/// 
/// Outside the Ref Connector, these 'extra' settings would be input by the customer
/// via the connector config page in RecordPoint. The settings would then be accessible
/// to developers via an entry in: ConnectorConfigModel.Properties
/// </remarks>
public class ConnectorConfigOptions
{
    public static string SECTION_NAME = "ConnectorConfigs";

    /// <summary>
    /// ID of the connector config within Records365. 
    /// </summary>
    public required string ConnectorId { get; set; }

    /// <summary>
    /// ID of the tenant that owns this connector config.
    /// </summary>
    public required string TenantId { get; set; }

    /// <summary>
    /// Domain name of the tenant that owns this connector config.
    /// </summary>
    public required string TenantDomainName { get; set; }
    
    /// <summary>
    /// Full path of the root directory whose contents should be monitored.
    /// </summary>
    public required string Directory { get; set; }

    /// <summary>
    /// Whether all subdirectories should be submitted (bar specified exclusions)
    /// or no subdirectories should be submitted (bar specified inclusions).
    /// </summary>
    public IngestionMode DefaultIngestionMode { get; set; } = IngestionMode.All;

    /// <summary>
    /// List of subdirectories (i.e. Channels) that should not be submitted to RecordPoint.
    /// </summary>
    /// <remarks>For use with DefaultIngestionMode = All.</remarks>
    public List<string>? Excluded { get; set; } = null;

    /// <summary>
    /// List of subdirectories (i.e. Channels) that should be submitted to RecordPoint.
    /// </summary>
    /// <remarks>For use with DefaultIngestionMode = Selected.</remarks>
    public List<string>? Included { get; set; } = null;

    /// <summary>
    /// Whether content registration should be run for this connector.
    /// I.e. When None is selected only content created after the connector config was enabled will be ingested (via Content Synchronisation).
    /// When All is selected all historical content will be ingested.
    /// </summary>
    public ContentRegMode ContentRegistrationMode { get; set; } = ContentRegMode.All;
   public enum IngestionMode
    {
        All,
        Selected
    }
    public enum ContentRegMode
    {
        All,
        None
    }
}


