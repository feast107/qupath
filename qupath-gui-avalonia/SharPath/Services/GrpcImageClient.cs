using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using Grpc.Net.Client;
using Google.Protobuf;
using QuPath.Api;

namespace SharPath.Services
{
    public class GrpcImageClient : IDisposable
    {
        private readonly GrpcChannel _channel;
        private readonly ImageService.ImageServiceClient _client;

        public GrpcImageClient(string address)
        {
            _channel = GrpcChannel.ForAddress(address);
            _client = new ImageService.ImageServiceClient(_channel);
        }

        public async Task<string> ImportSlideAsync(string filePath)
        {
            using var call = _client.UploadSlide();
            using var fs = File.OpenRead(filePath);
            var buffer = new byte[64 * 1024];
            int read;
            while ((read = await fs.ReadAsync(buffer.AsMemory(0, buffer.Length))) > 0)
            {
                var chunk = new UploadChunk { Data = ByteString.CopyFrom(buffer, 0, read) };
                await call.RequestStream.WriteAsync(chunk);
            }
            await call.RequestStream.CompleteAsync();
            var resp = await call.ResponseAsync;
            return resp.SlideId;
        }

        public async IAsyncEnumerable<TileChunk> StreamTileAsync(string slideId, int level, int x, int y)
        {
            var req = new TileRequest
            {
                Image = new ImageId { Id = slideId },
                Level = level,
                X = x,
                Y = y
            };

            using var call = _client.StreamTile(req);
            while (await call.ResponseStream.MoveNext(CancellationToken.None))
            {
                var chunk = call.ResponseStream.Current;
                yield return chunk;
            }
        }

        public void Dispose()
        {
            _channel?.Dispose();
        }
    }
}
