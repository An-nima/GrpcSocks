namespace GrpcSocks.SocksCore
{
    public enum SocksVersion
    {
        Socks5 = 0x05
    }
    public enum AuthMethod
    {
        NoAuth = 0x00
    }
    public enum RequestCommand
    {
        Connect = 0x01,
        Bind = 0x02,
        UDP = 0x03
    }
    public enum AddressType
    {
        Ipv4 = 0x01,
        Domain = 0x03,
        Ipv6 = 0x04
    }
    public enum RequestResponse
    {
        Success = 0x00
    }
    public enum ProxyStep
    {
        NoAuth = 1,
        HasAuth = 2,
        HasConfirm = 3
    }
}
