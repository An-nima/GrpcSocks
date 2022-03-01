using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using GrpcSocks.Protos;
using GrpcSocks.SocksCore;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace GrpcSocks.Services
{
    public class SocksService : SocksStream.SocksStreamBase
    {
        public static ConcurrentDictionary<long, NetworkStream> MainNetworkStreams = new();
        public static Random MainRandom = new Random();
        public NetworkStream? ServerStream { get; set; }
        public IServerStreamWriter<BytesValue>? ResponseStream { get; set; }

        public override async Task<HandShakeResponse> Auth(BytesValue request, ServerCallContext context)
        {
            if (new MemoryStream(request.Value.ToArray()).Auth() == null)
            {
                Console.WriteLine("fail");
                return new HandShakeResponse
                {
                    Success = false
                };
            }
            return new HandShakeResponse
            {
                Success = true,
                ResponseBytes = ByteString.CopyFrom(ProxySteps.AcceptAuth())
            };
        }

        public override async Task<HandShakeResponse> Confirm(BytesValue request, ServerCallContext context)
        {
            var confirmStream = new MemoryStream(request.Value.ToArray());
            var requestHeader = confirmStream.ReceiveAuth();
            if (requestHeader == null)
            {
                Console.WriteLine("fail");
                return new HandShakeResponse
                {
                    Success = false
                };
            }
            requestHeader.SetHost(confirmStream);
            var client = new TcpClient(new IPEndPoint(IPAddress.Parse("0.0.0.0"), 0));
            var ipAddr = (await Dns.GetHostEntryAsync(requestHeader.AddressString!)).AddressList[0];
            await client.ConnectAsync(requestHeader.AddressString!, requestHeader.PortInt32);
            var upStream = client.GetStream();
            var upStreamID = MainRandom.NextInt64();
            Console.WriteLine($"{client.Client.LocalEndPoint} -> {requestHeader.AddressString}:{requestHeader.PortInt32} -- {upStreamID}");
            var upStreamIDBytes = BitConverter.GetBytes(upStreamID);
            MainNetworkStreams.TryAdd(upStreamID, upStream);
            _ = Task.Run(async () => { await Task.Delay(3000); MainNetworkStreams.TryRemove(upStreamID, out NetworkStream? _); });
            return new HandShakeResponse
            {
                Success = true,
                ResponseBytes = ByteString.CopyFrom(ProxySteps.AcceptRequest(requestHeader)),
                UpStreamID =  ByteString.CopyFrom(upStreamIDBytes)
            };
        }

        public override async Task Upload(IAsyncStreamReader<BytesValue> requestStream, IServerStreamWriter<BytesValue> responseStream, ServerCallContext context)
        {
            ResponseStream = responseStream;
            var first = true;
            await foreach (var byteString in requestStream.ReadAllAsync())
            {
                if (first)
                {
                    var upStreamID = BitConverter.ToInt64(byteString.Value.Span);
                    if (MainNetworkStreams.TryRemove(upStreamID, out NetworkStream? upStream))
                    {
                        ServerStream = upStream;
                        _ = ForwardResponse();
                        first = false;
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    if (byteString.Value.Memory.Length == 0) break;
                    await ServerStream!.WriteAsync(byteString.Value.Memory);
                }
            }
            GC.Collect();
        }
        public async Task ForwardResponse()
        {
			var bytes = new byte[1024*8];
			int byteCount;
			while ((byteCount = await ServerStream!.ReadAsync(bytes, 0, bytes.Length)) > 0)
			{
				await ResponseStream!.WriteAsync(new BytesValue { Value = UnsafeByteOperations.UnsafeWrap(new ReadOnlyMemory<byte>(bytes, 0, byteCount)) });
			}
            GC.Collect();
		}
    }
}
