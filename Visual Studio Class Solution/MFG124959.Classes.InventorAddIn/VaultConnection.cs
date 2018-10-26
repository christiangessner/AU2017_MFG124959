using System;
using System.IO;
using System.Linq;

namespace MFG124959.Classes.InventorAddIn
{
    internal class VaultConnection
    {
        internal static object GetVaultConnection(Inventor.Application application)
        {
            try
            {
                var fileName = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                var directoryName = Path.GetDirectoryName(fileName);
                if (directoryName == null)
                    return null;

                var edmAddinDll = Path.Combine(directoryName, 
                    "Connectivity.InventorAddin.EdmAddin.dll");

                if (!File.Exists(edmAddinDll))
                {
                    Console.WriteLine(@"EdmAddin not installed!");
                    return null;
                }

                var assembly = System.Reflection.Assembly.LoadFrom(edmAddinDll);
                var edmSecurity = assembly.GetTypes().First(c => c.Name.Contains("EdmSecurity"));

                var propInstance = edmSecurity.GetProperty(
                    "Instance",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                var instance = propInstance.GetValue(null, null);

                var methodInfo = instance.GetType().GetMethod("IsSignedIn");
                var isSignedIn = methodInfo.Invoke(instance, null);
                if (isSignedIn == null || Convert.ToBoolean(isSignedIn) != true)
                    application.CommandManager.ControlDefinitions["LoginCmdIntName"].Execute();

                var propVaultConnection = instance.GetType()
                    .GetProperty(
                        "VaultConnection",
                        System.Reflection.BindingFlags.Instance | 
                        System.Reflection.BindingFlags.Public);

                var vaultConnection = propVaultConnection.GetValue(instance, null);
                return vaultConnection;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return null;
        }
    }
}