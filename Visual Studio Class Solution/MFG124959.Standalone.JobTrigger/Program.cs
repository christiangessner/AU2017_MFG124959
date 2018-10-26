using System;
using System.Collections.Generic;
using System.Configuration;
using System.Reflection;
using Autodesk.Connectivity.WebServices;
using Autodesk.Connectivity.WebServicesTools;

namespace MFG124959.Standalone.JobTrigger
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var assemblyName = Assembly.GetExecutingAssembly().GetName();
            Console.WriteLine("{0} Version {1}", assemblyName.Name, assemblyName.Version);

            #region Console arguments
            if (args.Length != 3)
            {
                Console.WriteLine("Please specify 'job type', 'job description' and 'job priority' as command line arguments!");
                return;
            }

            var jobType = args[0];
            var jobDesc = args[1];
            var jobPriority = Convert.ToInt32(args[2]);
            #endregion

            #region Setting from App.config
            var fileMap = new ExeConfigurationFileMap
            {
                ExeConfigFilename = Assembly.GetExecutingAssembly().Location + ".config"
            };
            var configuration = ConfigurationManager.OpenMappedExeConfiguration(
                fileMap, ConfigurationUserLevel.None);
            var section = configuration.GetSection("Settings/Vault") as AppSettingsSection;

            if (section == null)
                throw new ConfigurationErrorsException("Application Config file");

            var dataServer = section.Settings["DataServer"].Value;
            var fileServer = section.Settings["FileServer"].Value;
            var vault = section.Settings["Vault"].Value;
            var username = section.Settings["Username"].Value;
            var password = section.Settings["Password"].Value;
            #endregion
            
            try
            {
                Console.WriteLine("Vault '{0}', User '{1}'...", vault, username);
                
                var identities = new ServerIdentities
                {
                    DataServer = dataServer, FileServer = fileServer
                };
                var userPasswordCridentials = new UserPasswordCredentials(
                    identities, vault, username, password, true);
                var wsm = new WebServiceManager(userPasswordCridentials);

                wsm.JobService.AddJob(
                    jobType, 
                    jobDesc, 
                    new List<JobParam>().ToArray(), 
                    jobPriority);
                wsm.AuthService.SignOut();

                Console.WriteLine("Added job '{0}' to job queue!", jobType);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Console.ReadLine();
            }
        }
    }
}