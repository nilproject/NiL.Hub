using System.IO;
using System.Threading.Tasks;

namespace NiL.Hub
{
    public static class RemoteStreamUtils
    {
        public static string RegisterRemoteStream(this Stream stream)
        {
            var streamId = Hub._currentHub.RegisterStream(stream);
            return streamId + "@" + Hub._currentHub.Id;
        }

        public static Task<string> RegisterRemoteStream(this Task<Stream> stream)
        {
            var hub = Hub._currentHub;
            return stream.ContinueWith(x =>
            {
                var streamId = hub.RegisterStream(x.Result);
                return streamId + "@" + hub.Id;
            });
        }

        public static Task<RemoteStream> GetRemoteStream(this Hub hub, string streamToken)
        {
            var split = streamToken.Split('@', 2);
            return hub.GetRemoteStream(long.Parse(split[1]), int.Parse(split[0]));
        }
    }
}
