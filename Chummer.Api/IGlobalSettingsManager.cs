using Chummer.Api.Models.GlobalSettings;

namespace Chummer.Api
{
    public interface IGlobalSettingsManager
    {
        GlobalSettings LoadGlobalSettings(Stream stream);
        void SerializeGlobalSettings(GlobalSettings globalSettings, Stream stream);
    }
}
