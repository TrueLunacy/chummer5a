using Chummer.Api.Models.GlobalSettings;

namespace Chummer.Api
{
    public class OtherOsLegacySettingsManager(IGlobalSettingsManager manager) : ILegacySettingsManager
    {
        public GlobalSettings LoadLegacyRegistrySettings()
        {
            return manager.DefaultGlobalSettings;
        }
    }
}
