using Grpc.Core;
using Grpc.Core.Interceptors;
using System.Numerics;
using System.Security.Cryptography;

namespace GrpcSocks.Interceptors
{
	public class ClientInterceptor : Interceptor
	{
		public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(ClientInterceptorContext<TRequest, TResponse> context, AsyncDuplexStreamingCallContinuation<TRequest, TResponse> continuation)
		{
			var newMetadata = new Metadata();
			var now = DateTime.UtcNow.ToBinary();
			var salt = new BigInteger(Guid.Parse(SocksSettings.Guid!).ToByteArray());
			newMetadata.Add("time",now.ToString());
            newMetadata.Add("result", string.Join("", MD5.HashData(BigInteger.Add(salt, new BigInteger(now)).ToByteArray())));
            var newContext = new ClientInterceptorContext<TRequest, TResponse>(context.Method, context.Host, context.Options.WithHeaders(newMetadata));
			return base.AsyncDuplexStreamingCall(newContext, continuation);
		}
		public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context, AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
		{
			var newMetadata = new Metadata();
			var now = DateTime.UtcNow.ToBinary();
			var salt = new BigInteger(Guid.Parse(SocksSettings.Guid!).ToByteArray());
			newMetadata.Add("time", now.ToString());
			newMetadata.Add("result", string.Join("", MD5.HashData(BigInteger.Add(salt, new BigInteger(now)).ToByteArray())));
			var newContext = new ClientInterceptorContext<TRequest, TResponse>(context.Method, context.Host, context.Options.WithHeaders(newMetadata));
			return base.AsyncUnaryCall(request, newContext, continuation);
		}
    }
}
