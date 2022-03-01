namespace GrpcSocks.SocksCore
{
    public class HandshakeHeader
    {
        public SocksVersion Version { get; set; }
        public byte MethodCount { get; set; }
        public AuthMethod[]? Methods { get; set; }
    }
}
