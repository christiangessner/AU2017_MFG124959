using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using MFG124959.Classes.VaultInterface;

namespace MFG124959.Classes.InventorAddIn
{
    internal class AssemblyLoader
    {
        public IVaultFunctions LoadLateBindingAssemblies()
        {
            AddAssemblyResolvers();

            var interfaceType = typeof(IVaultFunctions);

            var assemblyName = Settings.ImplementationAssembly;
            if (assemblyName == null)
                throw new ConfigurationErrorsException(
                    "A configuration entry for 'VaultTranslatorAssembly' is missing!");

            var assemblyDirectory = Path.GetDirectoryName(Assembly.GetCallingAssembly().Location);
            if (assemblyDirectory == null)
                throw new ConfigurationErrorsException(
                    string.Format("Calling assembly '{0}' has no physical path!", assemblyName));

            var assemblyfullFileName = Path.Combine(assemblyDirectory, assemblyName);
            if (!File.Exists(assemblyfullFileName))
                throw new ConfigurationErrorsException(
                    string.Format("Assembly '{0}' doesn't exsist!", assemblyfullFileName));

            var assembly = Assembly.LoadFrom(assemblyfullFileName);
            var type = assembly.GetTypes().SingleOrDefault(t => t.GetInterfaces().
                Contains(interfaceType));
            if (type == null)
                throw new TypeLoadException(
                    string.Format("Assembly '{0}' doesn't implement the interface '{1}'!", 
                    assemblyName, interfaceType.Name));

            var instance = (IVaultFunctions)Activator.CreateInstance(type);
            if (instance == null)
                throw new TypeLoadException(
                    string.Format("Assembly '{0}' cannot be loaded!", assemblyfullFileName));

            return instance;
        }

        public void AddAssemblyResolvers()
        {
            AppDomain.CurrentDomain.AssemblyLoad += CurrentDomainAssemblyLoad;
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomainAssemblyResolve;
        }

        private static string _directory;
        static Assembly CurrentDomainAssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (_directory != null)
            {
                var fullFileName = Path.Combine(_directory, args.Name);
                return Assembly.LoadFrom(fullFileName);
            }
            return null;
        }

        static void CurrentDomainAssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            var assembly = args.LoadedAssembly;
            if (assembly != null && assembly.FullName.StartsWith(
                "Autodesk.Connectivity.WebServices"))
            {
                _directory = Path.GetDirectoryName(assembly.Location);
            }
        }
    }
}