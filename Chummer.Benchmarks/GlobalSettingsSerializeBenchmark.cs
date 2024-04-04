using BenchmarkDotNet.Attributes;
using Chummer.Api;

namespace Chummer.Benchmarks
{
    // Benchmark.NET complains because the out of runner process is run as Net8.0, not Net8.0 windows
    [InProcess]
    public class GlobalSettingsSerializeBenchmark
    {
        [Benchmark]
        public MemoryStream SerializeDefaultToMemoryStream()
        {
            MemoryStream ms = new MemoryStream();
            GlobalSettingsManager gsm = new GlobalSettingsManager();
            gsm.SerializeGlobalSettings(Api.Models.GlobalSettings.GlobalSettings.DefaultSettings, ms);
            return ms;
        }

        [Benchmark]
        public FileStream SerializeDefaultToFileStream()
        {
            FileStream fs = new FileStream(Path.GetTempFileName(), FileMode.Open);
            GlobalSettingsManager gsm = new GlobalSettingsManager();
            gsm.SerializeGlobalSettings(Api.Models.GlobalSettings.GlobalSettings.DefaultSettings, fs);
            fs.Flush();
            fs.Close();
            return fs;
        }

        public static readonly IReadOnlyList<DirectoryInfo> info = Enumerable
            .Repeat(new DirectoryInfo(Path.GetTempPath()), 150)
            .ToList();

        [Benchmark]
        public MemoryStream SerializeExtremeCase()
        {
            MemoryStream ms = new MemoryStream();
            GlobalSettingsManager gsm = new GlobalSettingsManager();
            Api.Models.GlobalSettings.GlobalSettings s = Api.Models.GlobalSettings.GlobalSettings.DefaultSettings;
            s = s with
            {
                CustomData = s.CustomData with
                {
                    CustomDataDirectories = info
                }
            };
            gsm.SerializeGlobalSettings(s, ms);
            return ms;
        }
    }
}
