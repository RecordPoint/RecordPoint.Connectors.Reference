using RecordPoint.Connectors.SDK.Caching.Semaphore;
using RecordPoint.Connectors.SDK.ContentManager;

namespace RecordPoint.Connectors.Reference.Common
{
    public class ContentHelper
    {
        public static ContentResult FailedResult(string cursor, Exception ex)
        {
            return new ContentResult
            {
                ResultType = ContentResultType.Failed,
                Cursor = cursor,
                Exception = ex
            };
        }
        /// <summary>
        /// Should be returned when the Channel no longer exists in the content source.
        /// </summary>
        /// <remarks>
        /// Signals to the SDK to stop operating on this Channel.
        /// </remarks>
        public static ContentResult AbandonResult(string reason)
        {
            return new()
            {
                Reason = reason,
                ResultType = ContentResultType.Abandonded,
                Cursor = string.Empty,
            };
        }
        public static ContentResult ThrottledContentResult(Exception ex, int delay)
        {
            return new ContentResult
            {
                ResultType = ContentResultType.BackOff,
                SemaphoreLockType = SemaphoreLockType.Scoped,
                Exception = ex,
                NextDelay = delay
            };
        }
    }
}
