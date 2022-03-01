using System.Net;
using System.Text;

namespace GrpcSocks.SocksCore
{
    public class RequestHeader
    {
        public SocksVersion Version { get; set; }
        public RequestCommand Command { get; set; }
        public byte RSV { get; set; } = 0;
        public AddressType AddressType { get; set; }
        public byte[]? Address { get; set; }
        public string? AddressString
        {
            get
            {
                if (Address == null) return null;
                switch (AddressType)
                {
                    case AddressType.Ipv4:
                        return new IPAddress(Address).ToString();
                    case AddressType.Domain:
                        return Encoding.ASCII.GetString(Address);
                    case AddressType.Ipv6:
                        return new IPAddress(Address).ToString();
                    default:
                        return null;
                }
            }
        }
        public byte[]? Port { get; set; }
        public int PortInt32
        {
            get
            {
                return BitConverter.ToInt32(Port?.Reverse().Append((byte)0).Append((byte)0).ToArray());
            }
        }
    }
}
