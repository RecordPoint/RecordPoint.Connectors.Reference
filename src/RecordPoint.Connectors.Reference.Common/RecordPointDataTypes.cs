namespace RecordPoint.Connectors.Reference.Common;

/// <summary>
/// MetaDataItem.Type values that are supported by RecordPoint.
/// </summary>
public static class RecordPointDataTypes
{
    public const string String = nameof(String);
    public const string Boolean = nameof(Boolean);
    public const string DateTime = nameof(DateTime);

    /// <summary>
    /// Should be used for all numeric fields
    /// </summary>
    public const string Double = nameof(Double);

}