using System.Globalization;
using HeyRed.Mime;
using RecordPoint.Connectors.SDK.Content;

namespace RecordPoint.Connectors.Reference.Common;

public static class SdkConverter
{
    /// <summary>
    /// Converts DirectoryInfo (aggregations in the content source) to Aggregation (the Connectors.SDK class).
    /// Returns whether the aggregation is an old version (i.e. should not be submitted).
    /// </summary>
    public static (Aggregation aggregation, bool isOldVersion) GetSdkAggregation(DirectoryInfo aggregation,
        Channel channel, DateTime? lastPolledTime = null)
    {
        var shouldSkip = ShouldSkip(aggregation, lastPolledTime);
        var author = GetAuthor(aggregation);

        var sdkAggregation = new Aggregation
        {
            ExternalId = GetExternalId(),
            Title = aggregation.Name, 
            Author = author,
            Location = aggregation.FullName,
            ContentVersion = null,
            SourceCreatedBy = author, // For other connectors, may not be the same as the author
            SourceCreatedDate = aggregation.CreationTimeUtc,
            SourceLastModifiedBy = author, // For other connectors, may not be the same as the author
            SourceLastModifiedDate = aggregation.LastWriteTimeUtc,
            ParentExternalId = channel.ExternalId,
            MetaDataItems = new List<MetaDataItem>
            {
                new()
                {
                    Name = MetadataNames.TimeSubmitted,
                    Type = RecordPointDataTypes.DateTime,
                    Value = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture) 
                }
            }
        };

        return (sdkAggregation, shouldSkip);
    }

    /// <summary>
    /// Converts FileInfos (a record in the content source) to Records (the Connectors.SDK class)
    /// </summary>
    public static (List<Record>, List<AuditEvent>) GetSdkRecords(IEnumerable<FileInfo> records, Aggregation parent,
        DateTime? lastPolledTime = null)
    {
        var sdkRecords = new List<Record>();
        var auditEvents = new List<AuditEvent>();
        foreach (var record in records)
        {
            if (ShouldSkip(record, lastPolledTime)) 
                continue;

            var author = GetAuthor(record);
            var externalId = GetExternalId();
            var title = Path.GetFileNameWithoutExtension(record.Name);
            var mimeType = GetMimeType(record);

            var sdkRecord = new Record
            {
                ExternalId = externalId,
                Title = title, // Should NOT contain file extension
                Author = author,
                Location = record.FullName,
                ContentVersion = "1.0", // For other connectors, use a version number from the content source
                SourceCreatedBy = author, // For other connectors, may not be the same as the author
                SourceCreatedDate = record.CreationTimeUtc,
                SourceLastModifiedBy = author, // For other connectors, may not be the same as the author
                SourceLastModifiedDate = record.LastWriteTimeUtc,
                ParentExternalId = parent.ExternalId,
                MimeType = mimeType, // If a record has multiple binaries, will have to decide which Mime Type to use
                MetaDataItems = new List<MetaDataItem>
                {
                    new()
                    {
                        Name = MetadataNames.TimeSubmitted,
                        Type = RecordPointDataTypes.DateTime,
                        Value = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture) 
                    },
                    new()
                    {
                        Name = MetadataNames.CheckedOutBy,
                        Type = RecordPointDataTypes.String,
                        Value = "John Doe"
                    }
                },
                Binaries = new List<BinaryMetaInfo>
                {
                    // Some content sources may have multiple binaries per record
                    // (e.g. an e-mail record => e-mail body and attachment binaries)
                    new() {
                        ItemExternalId = externalId,
                        ExternalId = externalId, // External ID of the binary; since there's only 1, just using item ID
                        Title = record.Name, // Should NOT contain file extension
                        ContentToken = record.FullName, // Used to locate the binary in BinaryRetrievalAction
                        MimeType = mimeType,
                        FileName = record.Name, // Should contain file extension
                        SourceLastModifiedDate = record.LastWriteTimeUtc, // In some content sources, records & their binaries may have different LastModifiedDates
                        FileSize = record.Length, // In bytes
                        MetaDataItems = new List<MetaDataItem>()

                        // If the content source provides an (MD5) hash of the binary,
                        // set the FileHash property. Otherwise skip.
                    }
                }
            };
            if(WasRecordCheckedOut(record))
            {
                auditEvents.Add(CreateRecordCheckedOutEvent(sdkRecord));
            }
            sdkRecords.Add(sdkRecord);
        }

        return (sdkRecords, auditEvents);
    }
    /// <summary>
    /// This is a sample method showing how to submit audit events. Most connectors will monitor other events
    /// (e.g. 'Deleted'), not 'Checked Out'. Some connectors do not even submit audits.
    /// Events like record creation and updating are tracked by the SDK already.
    /// </summary>
    /// <param name="sdkRecord"></param>
    /// <returns></returns>
    private static AuditEvent CreateRecordCheckedOutEvent(Record sdkRecord)
    {
        return new AuditEvent
        {
            CreatedDate = sdkRecord.SourceCreatedDate.DateTime,
            //This should match the User that set off the event
            UserName = sdkRecord.MetaDataItems.FirstOrDefault(x => x.Name == MetadataNames.CheckedOutBy)?.Value ?? "Unknown",
            UserId = "",
            Description = "Record Was Checked out",
            ExternalId = sdkRecord.ExternalId ?? string.Empty,
            //If the event has an external id from the source we can track it here
            EventExternalId = "",
            EventType = AuditEventNames.RecordCheckedOut,
            //Relevant Metadata can also be added to the event
            MetaDataItems = new List<MetaDataItem>
            {
                new()
                {
                    Name = MetadataNames.CheckedOutBy,
                    Type = RecordPointDataTypes.String,
                    Value = sdkRecord.MetaDataItems.FirstOrDefault(x => x.Name == MetadataNames.CheckedOutBy)?.Value ?? "Unknown"
                }
            }
        };
    }
    private static bool WasRecordCheckedOut(FileInfo record)
    {
        //Stub for demonstration purposes. Outside the Reference Connector you may need to perform some
        //logic on the file to determine if this record version is associated with an auditable event.
        return true;
    }
    private static bool ShouldSkip(FileSystemInfo sysInfo, DateTime? lastPolledTime)
    {
        // The Ref Connector uses the DateTime start of each polling loop as the cursor.
        // If items are edited during the polling loop, it might (wrongly) submit
        // the same item version in 2 separate loops.
        // (Most connectors won't have this issue. If necessary, create a cache
        // of recent item IDs / version IDs to help filter out duplicates.)
        var hasLastPollTime = lastPolledTime != null && lastPolledTime != DateTime.MinValue;
        return hasLastPollTime && sysInfo.LastWriteTimeUtc < lastPolledTime;
    }

    private static string GetAuthor(DirectoryInfo directory)
    {
        // Gets the e-mail or username of the user who created this aggregation.
        // (For the Ref Connector, we can only get the File Owner, which may not be the same as the
        // creator or last modifying user.)
        var owner = directory.GetAccessControl().GetOwner(typeof(System.Security.Principal.NTAccount));
        return owner?.ToString() ?? "Local User";
    }

    private static string GetAuthor(FileInfo file)
    {
        // Gets the e-mail or username of the user who created this record.
        // (For the Ref Connector, we can only use the File Owner, which may not be the same as the
        // creator or last modifying user.)
        var owner = file.GetAccessControl().GetOwner(typeof(System.Security.Principal.NTAccount));
        return owner?.ToString() ?? "Local User";
    }

    private static string GetExternalId()
    {
        // For other connectors, should be a field from the content source
        // which uniquely identifies the item within that source, and doesn't change between item versions
        return Guid.NewGuid().ToString();
    }

    private static string GetMimeType(FileInfo file)
    {
        // Ideally the content source will supply the MIME type
        // (Here, we have to guess it from the extension)
        var extension = Path.GetExtension(file.Name);
        return MimeTypesMap.GetMimeType(extension);
    }
}