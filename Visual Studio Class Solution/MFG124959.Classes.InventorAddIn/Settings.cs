using System.Configuration;
using System.Reflection;

namespace MFG124959.Classes.InventorAddIn
{
    internal static class Settings
    {
        private static AppSettingsSection _section;
        internal static AppSettingsSection Section
        {
            get
            {
                if (_section == null)
                {
                    var fileMap = new ExeConfigurationFileMap
                    {
                        ExeConfigFilename = Assembly.GetExecutingAssembly().Location + ".config"
                    };
                    var configuration = ConfigurationManager.OpenMappedExeConfiguration(
                        fileMap, ConfigurationUserLevel.None);
                    _section = configuration.GetSection("Settings/Vault") as AppSettingsSection;
                }

                return _section;
            }
        }

        internal static string ImplementationAssembly
        {
            get { return Section.Settings["ImplementationAssembly"].Value; }
        }
    }
}
