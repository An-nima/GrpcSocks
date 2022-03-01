using System.Net;
using System.Net.Sockets;

namespace GrpcSocks.SocksCore
{
    public static class ProxySteps
    {
        #region 认证
        public static HandshakeHeader? Auth(this MemoryStream stream)
        {
            var version = stream.ReadByte();
            if (version != 5) return null;
            var methodCount = stream.ReadByte();
            var methods = new byte[methodCount];
            stream.Read(methods, 0, methodCount);
            if (!(methodCount == 1 && methods.First() == 0)) return null;
            return new HandshakeHeader { Version = (SocksVersion)version, MethodCount = (byte)methodCount, Methods = methods.Select(x => (AuthMethod)x).ToArray() };
        }
        public static byte[] AcceptAuth()
        {
            var accecptSocks5 = new byte[] { (byte)SocksVersion.Socks5, (byte)AuthMethod.NoAuth };
            return accecptSocks5.ToArray();
        }
        #endregion


        #region 握手
        public static RequestHeader? ReceiveAuth(this MemoryStream stream)
        {
            var version = stream.ReadByte();
            if (version != 5) return null;
            var command = stream.ReadByte();
            var RSV = stream.ReadByte();
            if (!(command == 1 && RSV == 0)) return null;
            return new RequestHeader
            {
                Version = (SocksVersion)version,
                Command = (RequestCommand)command,
                RSV = (byte)RSV
            };
        }
        public static RequestHeader? SetHost(this RequestHeader requestHeader, MemoryStream stream)
        {
            requestHeader.AddressType = (AddressType)stream.ReadByte();
            switch (requestHeader.AddressType)
            {
                case AddressType.Ipv4:
                    var ipv4Bytes = new byte[4];
                    stream.Read(ipv4Bytes, 0, ipv4Bytes.Length);
                    requestHeader.Address = ipv4Bytes;
                    break;
                case AddressType.Domain:
                    var domainLength = stream.ReadByte();
                    var domainBytes = new byte[domainLength];
                    stream.Read(domainBytes, 0, domainBytes.Length);
                    requestHeader.Address = domainBytes;
                    break;
                case AddressType.Ipv6:
                    var ipv6Bytes = new byte[4];
                    stream.Read(ipv6Bytes, 0, ipv6Bytes.Length);
                    requestHeader.Address = ipv6Bytes;
                    break;
            }
            var portBytes = new byte[2];
            stream.Read(portBytes, 0, portBytes.Length);
            requestHeader.Port = portBytes;
            return requestHeader;
        }
        public static byte[] AcceptRequest(RequestHeader requestHeader)
        {
            var acceptBytes = new byte[]
            {
                (byte)SocksVersion.Socks5,(byte)RequestResponse.Success,0x00,(byte)requestHeader.AddressType
            }.AsEnumerable();
            if (requestHeader.AddressType == AddressType.Domain) acceptBytes = acceptBytes.Append((byte)requestHeader.Address!.Length);
            acceptBytes = acceptBytes.Concat(requestHeader.Address!).Concat(requestHeader.Port!);
            return acceptBytes.ToArray();
        }
        #endregion


    }
}
