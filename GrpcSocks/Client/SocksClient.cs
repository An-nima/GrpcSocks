using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using GrpcSocks.Extension;
using GrpcSocks.Interceptors;
using GrpcSocks.Protos;
using GrpcSocks.SocksCore;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using static GrpcSocks.Protos.SocksStream;

namespace GrpcSocks.Client
{
    public class SocksClient
    {
        public TcpListener? MainTcpListener { get; set; }
        public GrpcChannel? MainGrpcChannel { get; set; }
        public CallInvoker? MainCallInvoker { get; set; }
        public async Task StartAsync()
        {
            var localIP = IPAddress.Parse(SocksSettings.LocalClientBindAddr!);
            MainTcpListener = new TcpListener(localIP, SocksSettings.LocalClientBindPort);
            MainTcpListener.Start();
            Logger.WriteLine($"info: Local listen on {localIP}:{SocksSettings.LocalClientBindPort}");
            MainGrpcChannel = GrpcChannel.ForAddress($"https://{SocksSettings.ServerAddr!}:{SocksSettings.ServerPort!}", new GrpcChannelOptions
            {
                HttpClient = new HttpClient(new HttpClientHandler
                {
                    SslProtocols = System.Security.Authentication.SslProtocols.Tls13 | System.Security.Authentication.SslProtocols.Tls12
                })
                {
                    DefaultRequestVersion = HttpVersion.Version30,
                    DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower
                },
                MaxSendMessageSize = int.MaxValue
            });
            MainCallInvoker = MainGrpcChannel.Intercept(new ClientInterceptor());
            while (true)
            {
                var client = await MainTcpListener.AcceptTcpClientAsync();
                _ = GrpcStartAsync(client);
            }
        }
        public async Task GrpcStartAsync(TcpClient client)
        {
            var stream = client.GetStream();
            var socksStreamClient = new SocksStreamClient(MainCallInvoker);
            var upStreamID = await HandShakeAsync(stream, socksStreamClient);
            if (upStreamID == null) return;
            await GrpcUploadAsync(stream, socksStreamClient, upStreamID);
        }
        public async Task<ByteString?> HandShakeAsync(NetworkStream stream, SocksStreamClient socksStreamClient)
        {
            var authBytes = new byte[1024];
            var authByteCount = await stream.ReadAsync(authBytes, 0, authBytes.Length);
            var authCall = await socksStreamClient.AuthAsync(new BytesValue { Value = UnsafeByteOperations.UnsafeWrap(new ReadOnlyMemory<byte>(authBytes, 0, authByteCount)) });
            if (!authCall.Success!.Value) return null;
            stream.Write(authCall.ResponseBytes.ToByteArray());

            var confirmBytes = new byte[1024];
            var confirmBytesCount = await stream.ReadAsync(confirmBytes, 0, authBytes.Length);
            var confirmCall = await socksStreamClient.ConfirmAsync(new BytesValue { Value = UnsafeByteOperations.UnsafeWrap(new ReadOnlyMemory<byte>(confirmBytes, 0, confirmBytesCount)) });
            if (!confirmCall.Success!.Value) return null;
            stream.Write(confirmCall.ResponseBytes.ToByteArray());
            return confirmCall.UpStreamID;
        }

        public async Task GrpcUploadAsync(NetworkStream stream, SocksStreamClient socksStreamClient, ByteString upStreamID)
        {
            var call = socksStreamClient.Upload();
            _ = GrpcReceiveAsync(call, stream);
            var zeroByte = new byte[0];
            await call.RequestStream.WriteAsync(new BytesValue
            {
                Value = upStreamID
            });
            int byteCount;
            var bytes = new byte[1024];
            while ((byteCount = await stream.ReadAsync(bytes, 0, bytes.Length)) > 0)
            {
                await call.RequestStream.WriteAsync(new BytesValue
                {
                    Value = UnsafeByteOperations.UnsafeWrap(new ReadOnlyMemory<byte>(bytes, 0, byteCount))
                });
            }
            await call.RequestStream.WriteAsync(new BytesValue
            {
                Value = ByteString.CopyFrom(bytes.Take(0).ToArray())
            });
            GC.Collect();
        }
        public async Task GrpcReceiveAsync(AsyncDuplexStreamingCall<BytesValue, BytesValue> call, NetworkStream stream)
        {
            var response = call.ResponseStream.ReadAllAsync();
            await foreach (var byteValue in response)
            {
                await stream.WriteAsync(byteValue.Value.Memory);
                await stream.FlushAsync();
            }
            GC.Collect();
        }
    }
}
