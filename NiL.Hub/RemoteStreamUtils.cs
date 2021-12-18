using System;
using System.IO;
using System.Threading.Tasks;

namespace NiL.Hub
{
    public static class RemoteStreamUtils
    {
        public static string RegisterRemoteStream(this Stream stream)
        {
            return Hub._currentHub.RegisterRemoteStream(stream);
        }

        public static string RegisterRemoteStream(this IHub hub, Stream stream)
        {
            var streamId = hub.RegisterStream(stream);
            return streamId + "@" + hub.Id;
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

        public static Task<RemoteStream> GetRemoteStream(this IHub hub, string streamToken)
        {
            var split = streamToken.Split('@', 2);
            return hub.GetRemoteStream(long.Parse(split[1]), int.Parse(split[0]));
        }

        public static Task<RemoteStream> GetRemoteStream(string streamToken)
        {
            return Hub._currentHub.GetRemoteStream(streamToken);
        }

        public static bool UnregisterStream(this IHub hub, string streamToken)
        {
            var split = streamToken.Split('@', 2);
            if (hub.Id != long.Parse(split[1]))
                throw new InvalidOperationException();

            return hub.UnregisterStream(int.Parse(split[0]));
        }

        public static async Task Copy(Stream sourceStream, Stream targetStream, long minCopySize)
        {
            var buffer = new byte[short.MaxValue];
            var copySize = 0L;
            for (; ; )
            {
                var readed = await sourceStream.ReadAsync(buffer, 0, buffer.Length);

                if (readed == 0)
                {
                    if (copySize >= minCopySize)
                        break;

                    await Task.Delay(100);
                    continue;
                }

                await targetStream.WriteAsync(buffer, 0, readed);

                copySize += readed;
            }
        }
    }
}
