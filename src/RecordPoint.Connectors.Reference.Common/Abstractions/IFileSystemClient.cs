namespace RecordPoint.Connectors.Reference.Common.Abstractions;

public interface IFileSystemClient
{
    /// <summary>
    /// Gets a stream for the binary. 
    /// </summary>
    /// <returns>The stream or null if the binary does not exist.</returns>
    FileStream? GetBinaryStream(string path);

    /// <summary>
    /// Gets the channel (folder) with the given path.
    /// </summary>
    /// <returns>The folder or null if the folder does not exist.</returns>
    DirectoryInfo? GetChannel(string channelPath);

    /// <summary>
    /// Gets child aggregations (folders) from a channel (folder) with the given path.
    /// </summary>
    IEnumerable<DirectoryInfo> GetAggregations(string channelPath);

    /// <summary>
    /// Gets child records (files) from the given aggregation (folder).
    /// </summary>
    IEnumerable<FileInfo> GetRecords(DirectoryInfo aggregation);
}