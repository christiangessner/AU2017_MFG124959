using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Autodesk.Connectivity.WebServices;
using Autodesk.Connectivity.WebServicesTools;
using VDF = Autodesk.DataManagement.Client.Framework;

[assembly: Autodesk.Connectivity.Extensibility.Framework.ExtensionId("7ADC0766-F085-46d7-A2EB-C68F79CBF4E1")]
[assembly: Autodesk.Connectivity.Extensibility.Framework.ApiVersion("11.0")]

namespace MFG124959.InternalID.EventHandler
{
    public class WebServiceExtension : IWebServiceExtension
    {
        public void OnLoad()
        {
            DocumentService.AddFileEvents.Post += AddFileEvents_Post;
        }

        private void AddFileEvents_Post(object sender, AddFileCommandEventArgs e)
        {
            var service = sender as IWebService;
            if (service == null)
                return;

            var wsm = service.WebServiceManager;
            var connection = new VDF.Vault.Currency.Connections.Connection(
                wsm,
                wsm.WebServiceCredentials.VaultName,
                service.SecurityHeader.UserId,
                wsm.WebServiceCredentials.ServerIdentities.DataServer,
                VDF.Vault.Currency.Connections.AuthenticationFlags.Standard);

            var file = e.ReturnValue;

            var localFilePath = string.Empty;
            var fileExtension = System.IO.Path.GetExtension(file.Name);
            if (fileExtension == null ||
                !new[] {".ipt", ".iam", ".ipn", ".idw", ".dwg"}.Contains(fileExtension.ToLower())) 
                return;

            #region Application detection
            var isExplorer = false;
            var isCopyDesign = false;
            var isInventor = false;
            try
            {
                var entryAssembly = Assembly.GetEntryAssembly();
                if (entryAssembly != null && entryAssembly.Location.StartsWith(
                        @"C:\Program Files\Autodesk\Vault Professional 2018\Explorer\Connectivity.Vault", 
                        StringComparison.OrdinalIgnoreCase))
                {
                    isExplorer = true;

                    var regex = new Regex(@"Copy of file \'.*\' version \'.*'\. \(.*\)");
                    var match = regex.Match(file.Comm);
                    if (match.Success)
                        isCopyDesign = true;
                }
                else if (entryAssembly != null && entryAssembly.Location.Equals(
                             @"C:\Program Files\Autodesk\Vault Professional 2018\Explorer\CopyDesign.exe",
                             StringComparison.OrdinalIgnoreCase))
                {
                    isCopyDesign = true;
                }
                else
                {
                    var callingAssembly = Assembly.GetCallingAssembly();
                    if (callingAssembly.Location.Equals(
                        @"C:\Program Files\Autodesk\Inventor 2018\Bin\Autodesk.Connectivity.WebServices.dll",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        isInventor = true;
                    }                        
                }

            }
            catch (Exception)
            {
                isExplorer = false;
            }
            #endregion

            if (isInventor)
            {
                try
                {
                    #region Download file (VDF)
                    var fileIteration = new VDF.Vault.Currency.Entities.FileIteration(
                        connection, file);

                    var folderPathAbsolute = new VDF.Currency.FolderPathAbsolute(@"C:\temp\");
                    var acquireSettings = new VDF.Vault.Settings.AcquireFilesSettings(connection)
                    {
                        DefaultAcquisitionOption = 
                            VDF.Vault.Settings.AcquireFilesSettings.AcquisitionOption.Download, 
                        LocalPath = folderPathAbsolute
                    };
                    acquireSettings.AddEntityToAcquire(fileIteration);

                    var acquireFiles = connection.FileManager.AcquireFiles(acquireSettings);
                    var fileResult = acquireFiles.FileResults.First();
                    localFilePath = fileResult.LocalPath.FullPath;
                    #endregion

                    #region Download file (WebServiceManager)
                    //localFilePath = string.Format(@"C:\temp\{0}", file.Name);

                    //var fileSize = file.FileSize;
                    //var maxPartSize = wsm.FilestoreService.GetMaximumPartSize();
                    //var ticket = wsm.DocumentService.GetDownloadTicketsByMasterIds(
                    //    new[] { file.MasterId })[0];
                    //byte[] bytes;

                    //using (var stream = new System.IO.MemoryStream())
                    //{
                    //    var startByte = 0;
                    //    while (startByte < fileSize)
                    //    {
                    //        var endByte = startByte + maxPartSize;
                    //        if (endByte > fileSize)
                    //            endByte = fileSize;

                    //        using (var filePart =
                    //            wsm.FilestoreService.DownloadFilePart(
                    //            ticket.Bytes, startByte, endByte, true))
                    //        {
                    //            byte[] buffer = StreamToByteArray(filePart);
                    //            stream.Write(buffer, 0, buffer.Length);
                    //            startByte += buffer.Length;
                    //        }
                    //    }

                    //    bytes = new byte[stream.Length];
                    //    stream.Seek(0, System.IO.SeekOrigin.Begin);
                    //    stream.Read(bytes, 0, (int)stream.Length);

                    //    stream.Close();
                    //}

                    //System.IO.File.WriteAllBytes(localFilePath, bytes);
                    #endregion

                    #region Get InternalName and iProperty Class
                    var documentInfo = Apprentice.GetDocumentInfo(localFilePath);
                    #endregion

                    #region Update properties
                    var propDefs = wsm.PropertyService.GetPropertyDefinitionsByEntityClassId("FILE");
                    var propDef = propDefs.SingleOrDefault(p => p.DispName.Equals("Internal ID"));
                    if (propDef == null)
                        return;

                    var util = Autodesk.Connectivity.Explorer.ExtensibilityTools.ExplorerLoader.LoadExplorerUtil(
                        wsm.WebServiceCredentials.ServerIdentities.DataServer,
                        wsm.WebServiceCredentials.VaultName,
                        service.SecurityHeader.UserId,
                        service.SecurityHeader.Ticket);

                    var properties = new Dictionary<PropDef, object> {{propDef, documentInfo.InternalName}};
                    util.UpdateFileProperties(file, properties);
                    #endregion

                    #region Assign file to custom entity 'Class'
                    if (!string.IsNullOrEmpty(documentInfo.Class))
                    {
                        var custEnt = GetClassCustEntByName(wsm, documentInfo.Class);
                        if (custEnt != null)
                            wsm.DocumentService.AddLink(custEnt.Id, "FILE", file.Id, null);
                    }
                    #endregion
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
                finally
                {
                    #region Delete local file
                    if (System.IO.File.Exists(localFilePath))
                    {
                        try
                        {
                            System.IO.File.Delete(localFilePath);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
                    }
                    #endregion
                }
            }

            if (isExplorer)
            {
                // file is added by Vault Explorer
            }

            if (isCopyDesign)
            {
                // file is added by CopyDesign
            }
        }

        //private byte[] StreamToByteArray(System.IO.Stream stream)
        //{
        //    using (var memoryStream = new System.IO.MemoryStream())
        //    {
        //        stream.CopyTo(memoryStream);
        //        return memoryStream.ToArray();
        //    }
        //}

        private CustEnt GetClassCustEntByName(WebServiceManager wsm, string name)
        {
            var custEnts = new List<CustEnt>();

            var custEntDefs = wsm.CustomEntityService.GetAllCustomEntityDefinitions();
            var custEntDef = custEntDefs.SingleOrDefault(c => c.DispName.Equals("Class"));
            if (custEntDef == null) return custEnts.FirstOrDefault();

            var propDefs = wsm.PropertyService.GetPropertyDefinitionsByEntityClassId("CUSTENT");
            var propDefDefinition = propDefs.Single(p => p.SysName.Equals("CustomEntitySystemName"));
            var propDefName = propDefs.Single(p => p.SysName.Equals("Name"));

            var srcCondDefinition = new SrchCond
            {
                PropDefId = propDefDefinition.Id,
                PropTyp = PropertySearchType.SingleProperty,
                SrchOper = 3,
                SrchRule = SearchRuleType.Must,
                SrchTxt = custEntDef.Name
            };

            var srcCondName = new SrchCond
            {
                PropDefId = propDefName.Id,
                PropTyp = PropertySearchType.SingleProperty,
                SrchOper = 3,
                SrchRule = SearchRuleType.Must,
                SrchTxt = name
            };

            string bookmark = null;
            SrchStatus status = null;
            while (status == null || custEnts.Count < status.TotalHits)
            {
                var results = wsm.CustomEntityService.FindCustomEntitiesBySearchConditions(
                    new[] {srcCondDefinition, srcCondName}, null, ref bookmark, out status);

                if (results != null)
                    custEnts.AddRange(results);
                else
                    break;
            }

            return custEnts.FirstOrDefault();
        }
    }
}