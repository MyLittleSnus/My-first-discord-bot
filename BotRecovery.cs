using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace Bot.Rebooting
{
    public class BotRecovery
    {
        public DateTime NextInvokeCheckBirthday { get; set; }

        [NonSerialized]
        public IConfiguration ConfigBuilder;

        public string filePath;

        public BotRecovery() { }

        public BotRecovery(IConfiguration configuration)
        {
            ConfigBuilder = configuration;
            filePath = ConfigBuilder
                .GetSection("BotSettings")
                .GetSection("BackupFile")
                .Value;
        }

        public void Save()
        {
            var serialized = JsonConvert.SerializeObject(this);

            File.WriteAllText(filePath, serialized);
        }

        public BotRecovery Load()
        {
            if (!File.Exists(filePath))
                Save();

            var content = File.ReadAllText(filePath);

            return JsonConvert.DeserializeObject<BotRecovery>(content);
        }
    }
}