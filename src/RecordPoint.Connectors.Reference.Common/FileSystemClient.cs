using RecordPoint.Connectors.Reference.Common.Abstractions;

namespace RecordPoint.Connectors.Reference.Common
{
    /// <summary>
    /// Performs most operations on the content source
    /// (i.e. for the Reference Connector, the file system).
    /// </summary>
    /// <remarks>
    /// Outside the Reference Connector, would be using APIs, SDK methods etc. from the content source
    /// to implement a similar client class.
    /// </remarks>
    public class FileSystemClient : IFileSystemClient
    {
        public FileStream? GetBinaryStream(string path)
        {
            if(!File.Exists(path))
            {
                return null;
            }

            var stream = File.OpenRead(path);
            return stream;
        }

        public DirectoryInfo? GetChannel(string channelPath)
        {
            var channelDir = new DirectoryInfo(channelPath);

            return channelDir.Exists ? channelDir : null;
        }

        public IEnumerable<DirectoryInfo> GetAggregations(string channelPath)
        {
            var channelDir = new DirectoryInfo(channelPath);
            return channelDir.EnumerateDirectories();
        }

        public IEnumerable<FileInfo> GetRecords(DirectoryInfo aggregation)
        {
            return aggregation.EnumerateFiles();
        }
    }
}
