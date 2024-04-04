using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Chummer.Api;

namespace Chummer.Benchmarks
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var gs = LegacySettingsManager.LoadLegacyRegistrySettings() ?? throw new InvalidOperationException();
            MemoryStream ms = new();
            var gsm = new GlobalSettingsManager();
            gsm.SerializeGlobalSettings(gs, ms);
            ms.Seek(0, SeekOrigin.Begin);
            using StreamReader sr = new StreamReader(ms);
            Console.WriteLine(sr.ReadToEnd());
            //var summary = BenchmarkRunner.Run<GlobalSettingsDeserializeBenchmark>();
        }
    }

    // Benchmark.NET complains because the out of runner process is run as Net8.0, not Net8.0 windows
    [InProcess]
    public class GlobalSettingsDeserializeBenchmark
    {
        [GlobalSetup]
        public void GlobalSetup()
        {
            using var ms = new MemoryStream();
            var gsm = new GlobalSettingsManager();
            gsm.SerializeGlobalSettings(Api.Models.GlobalSettings.GlobalSettings.DefaultSettings, ms);
            byteArray = ms.ToArray();

            settingsFile = new FileInfo(Path.GetTempFileName());
            using var fs = settingsFile.OpenWrite();
            gsm.SerializeGlobalSettings(Api.Models.GlobalSettings.GlobalSettings.DefaultSettings, fs);
        }

        private byte[] byteArray = default!;
        private FileInfo settingsFile = default!;
        private Stream MemoryStream => new MemoryStream(byteArray, false);

        [Benchmark]
        public Api.Models.GlobalSettings.GlobalSettings DeserializeDefaultFromMemoryStream()
        {
            GlobalSettingsManager manager = new();
            return manager.LoadGlobalSettings(MemoryStream);
        }

        [Benchmark]
        public Api.Models.GlobalSettings.GlobalSettings DeserializeDefaultFromFile()
        {
            GlobalSettingsManager manager = new();
            using var fs = settingsFile.OpenRead();
            return manager.LoadGlobalSettings(fs);
        }
    }
}
