using Grpc.Core;
using Grpc.Core.Interceptors;
using System.Numerics;
using System.Security.Cryptography;

namespace GrpcSocks.Interceptors
{
	public class ServerInterceptor:Interceptor
	{
        public override Task DuplexStreamingServerHandler<TRequest, TResponse>(IAsyncStreamReader<TRequest> requestStream, IServerStreamWriter<TResponse> responseStream, ServerCallContext context, DuplexStreamingServerMethod<TRequest, TResponse> continuation)
        {
            var time = Convert.ToInt64(context.RequestHeaders.Get("time")?.Value);
            if (DateTime.UtcNow - DateTime.FromBinary(time) > TimeSpan.FromSeconds(10)) throw new Exception("NoAuth");
            var salt = new BigInteger(Guid.Parse(SocksSettings.Guid!).ToByteArray());
            var result = string.Join("", MD5.HashData(BigInteger.Add(salt, new BigInteger(time)).ToByteArray()));
            if (context.RequestHeaders.Get("result")?.Value != result) throw new Exception("NoAuth");
            return base.DuplexStreamingServerHandler(requestStream, responseStream, context, continuation);
        }
        public override Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
        {
            var time = Convert.ToInt64(context.RequestHeaders.Get("time")?.Value);
            if (DateTime.UtcNow - DateTime.FromBinary(time) > TimeSpan.FromSeconds(10)) throw new Exception("NoAuth");
            var salt = new BigInteger(Guid.Parse(SocksSettings.Guid!).ToByteArray());
            var result = string.Join("", MD5.HashData(BigInteger.Add(salt, new BigInteger(time)).ToByteArray()));
            if (context.RequestHeaders.Get("result")?.Value != result) throw new Exception("NoAuth");
            return base.UnaryServerHandler(request, context, continuation);
        }
    }
}
