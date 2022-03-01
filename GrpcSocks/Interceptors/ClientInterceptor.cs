using Grpc.Core;
using Grpc.Core.Interceptors;
using System.Security.Cryptography;

namespace GrpcSocks.Interceptors
{
	public class ClientInterceptor : Interceptor
	{
		public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(ClientInterceptorContext<TRequest, TResponse> context, AsyncDuplexStreamingCallContinuation<TRequest, TResponse> continuation)
		{
			var newMetadata = new Metadata();
			var now = DateTime.UtcNow.ToBinary();
			var salt = 741092357197509;
			newMetadata.Add("time",now.ToString());
			newMetadata.Add("result", string.Join("", MD5.HashData(BitConverter.GetBytes(now + salt))));
			var newContext = new ClientInterceptorContext<TRequest, TResponse>(context.Method, context.Host, context.Options.WithHeaders(newMetadata));
			return base.AsyncDuplexStreamingCall(newContext, continuation);
		}
	}
}
