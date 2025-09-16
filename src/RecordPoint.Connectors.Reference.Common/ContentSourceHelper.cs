namespace RecordPoint.Connectors.Reference.Common;

public static class ContentSourceHelper
{
    public static bool IsThrottlingException(Exception ex)
    {
        // For the Reference Connector (which probably won't be throttled),
        // these exception types are picked arbitrarily.
        // Other content sources will likely throw a particular exception type.
        return ex is IOException or TimeoutException;
    }
    
    public static int GetBackoffTimeSeconds(Exception ex)
    {
        // This represents the time in seconds to wait before retrying the content reg.
        // If we pass this to the SDK, it will handle the backoff and retry logic for us.

        // Outside the Reference Connector, this value should (ideally) come from the content source
        // (e.g. from the exception).
        return 30;
    }
}