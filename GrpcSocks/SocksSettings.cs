using System.Text.Json;

namespace GrpcSocks
{
    public static class SocksSettings
    {
        public static string? Mode { get; set; }

        public static string? LocalClientBindAddr { get; set; }
        public static int LocalClientBindPort { get; set; }
        public static string? ServerAddr { get; set; }
        public static int ServerPort { get; set; }

        public static string? LocalServerBindAddr { get; set; }
        public static int LocalServerBindPort { get; set; }
        public static string? DomainName { get; set; }

        public static string? Guid { get; set; }
    }
    public class SerializeSocksSettings
    {
        public string? Mode { get; set; }

        public string? LocalClientBindAddr { get; set; }
        public int LocalClientBindPort { get; set; }
        public string? ServerAddr { get; set; }
        public int ServerPort { get; set; }

        public string? LocalServerBindAddr { get; set; }
        public int LocalServerBindPort { get; set; }
        public string? DomainName { get; set; }

        public string? Guid { get; set; }
    }
    public static class SocksSettingsExtension
    {
        public static async Task SetStatic()
        {
            var obj = await GetSocksSettings();
            foreach(var prop in typeof(SocksSettings).GetProperties())
            {
                prop.SetValue(typeof(SocksSettings),obj.GetType().GetProperty(prop.Name)!.GetValue(obj));
            }
        }
        public static async Task<SerializeSocksSettings> GetSocksSettings()
        {
            using var jsonFile = File.OpenRead("SocksSettings.cs.json");
            using var reader = new StreamReader(jsonFile);
            var text = await reader.ReadToEndAsync();
            var res = JsonSerializer.Deserialize<SerializeSocksSettings>(text);
            return res!;
        }
    }
}
