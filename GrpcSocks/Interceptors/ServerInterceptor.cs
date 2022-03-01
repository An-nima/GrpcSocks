using Grpc.Core;
using Grpc.Core.Interceptors;
using System.Security.Cryptography;

namespace GrpcSocks.Interceptors
{
	public class ServerInterceptor:Interceptor
	{
		public override Task DuplexStreamingServerHandler<TRequest, TResponse>(IAsyncStreamReader<TRequest> requestStream, IServerStreamWriter<TResponse> responseStream, ServerCallContext context, DuplexStreamingServerMethod<TRequest, TResponse> continuation)
		{
			var time = Convert.ToInt64(context.RequestHeaders.Get("time")?.Value);
			var salt = 741092357197509;
			var result = string.Join("",MD5.HashData(BitConverter.GetBytes(time + salt)));
			if (context.RequestHeaders.Get("result")?.Value != result && context.GetHttpContext().Connection.RemoteIpAddress?.ToString()!="::1") throw new Exception("未认证");
			return base.DuplexStreamingServerHandler(requestStream, responseStream, context, continuation);
		}
	}
}
