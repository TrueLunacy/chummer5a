using Chummer.Api.Models.GlobalSettings;

namespace Chummer.Api
{
    public interface ILegacySettingsManager
    {
        public GlobalSettings LoadLegacyRegistrySettings();
    }
}
