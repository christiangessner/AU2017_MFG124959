using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Connectivity.JobProcessor.Extensibility;
using Autodesk.Connectivity.WebServices;

[assembly: Autodesk.Connectivity.Extensibility.Framework.ExtensionId("8f14fa1e-e5db-4dee-ad56-98483dcc2a1d")]
[assembly: Autodesk.Connectivity.Extensibility.Framework.ApiVersion("11.0")]

namespace MFG124959.InternalID.Job
{
    public class JobHandler : IJobHandler
    {
        public JobOutcome Execute(IJobProcessorServices context, IJob job)
        {
            var wsm = context.Connection.WebServiceManager;

            #region Property Definitions
            var propDefs = wsm.PropertyService.GetPropertyDefinitionsByEntityClassId("FILE");
            var propDefInternalId = propDefs.Single(n => n.DispName == "Internal ID");
            var propDefProvider = propDefs.Single(n => n.DispName == "Provider");
            var propDefOriginalCreateDate = propDefs.Single(n => n.DispName == "Original Create Date");
            #endregion

            #region Search Conditions
            var srchCondInternalId = new SrchCond
            {
                PropDefId = propDefInternalId.Id,
                PropTyp = PropertySearchType.SingleProperty,
                SrchOper = 4, // Is empty
                SrchRule = SearchRuleType.Must
            };

            var srchCondInternalProvider = new SrchCond
            {
                PropDefId = propDefProvider.Id,
                PropTyp = PropertySearchType.SingleProperty,
                SrchOper = 3, // Is exactly (or equals)
                SrchRule = SearchRuleType.Must,
                SrchTxt = "Inventor"
            };

            var srchCondDateTime = new SrchCond
            {
                PropDefId = propDefOriginalCreateDate.Id,
                PropTyp = PropertySearchType.SingleProperty,
                SrchOper = 7, // Greater than or equal to
                SrchRule = SearchRuleType.Must,
                SrchTxt = DateTime.Now.AddDays(-15).ToUniversalTime().ToString(
                    "MM/dd/yyyy HH:mm:ss")
            };
            #endregion

            string bookmark = null;
            SrchStatus status = null;
            var files = new List<File>();
            while (status == null || files.Count < status.TotalHits)
            {
                var results = wsm.DocumentService.FindFilesBySearchConditions(
                    new[] { srchCondInternalId, srchCondInternalProvider, srchCondDateTime },
                    null, 
                    null, 
                    false, 
                    true, 
                    ref bookmark, 
                    out status);

                if (results != null)
                    files.AddRange(results);
                else
                    break;
            }

            foreach (var file in files)
            {
                var localFilePath = string.Format(@"C:\temp\{0}", file.Name);
                try
                {
                    #region Download file
                    var fileSize = file.FileSize;
                    var maxPartSize = wsm.FilestoreService.GetMaximumPartSize();
                    var ticket = wsm.DocumentService.GetDownloadTicketsByMasterIds(
                        new[] { file.MasterId })[0];
                    byte[] bytes;

                    using (var stream = new System.IO.MemoryStream())
                    {
                        var startByte = 0;
                        while (startByte < fileSize)
                        {
                            var endByte = startByte + maxPartSize;
                            if (endByte > fileSize)
                                endByte = fileSize;

                            using (var filePart = wsm.FilestoreService.DownloadFilePart(
                                ticket.Bytes, startByte, endByte, true))
                            {
                                byte[] buffer = StreamToByteArray(filePart);
                                stream.Write(buffer, 0, buffer.Length);
                                startByte += buffer.Length;
                            }
                        }

                        bytes = new byte[stream.Length];
                        stream.Seek(0, System.IO.SeekOrigin.Begin);
                        stream.Read(bytes, 0, (int)stream.Length);

                        stream.Close();
                    }

                    System.IO.File.WriteAllBytes(localFilePath, bytes);
                    #endregion

                    #region Get InternalName
                    var internalId = Apprentice.GetInternalId(localFilePath);
                    #endregion 

                    #region Update properties
                    var util = Autodesk.Connectivity.Explorer.ExtensibilityTools.ExplorerLoader.
                        LoadExplorerUtil(
                        wsm.WebServiceCredentials.ServerIdentities.DataServer,
                        wsm.WebServiceCredentials.VaultName,
                        context.Connection.UserID,
                        context.Connection.Ticket);

                    var properties = new Dictionary<PropDef, object>
                    {
                        { propDefInternalId, internalId }
                    };
                    util.UpdateFileProperties(file, properties);
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

            return JobOutcome.Success;
        }

        #region IJobHandler members
        public bool CanProcess(string jobType)
        {
            if (jobType.ToLower().Equals("MFG124959.InternalID.Job".ToLower()))
                return true;

            return false;
        }

        public void OnJobProcessorStartup(IJobProcessorServices context)
        {
        }

        public void OnJobProcessorShutdown(IJobProcessorServices context)
        {
        }

        public void OnJobProcessorWake(IJobProcessorServices context)
        {
        }

        public void OnJobProcessorSleep(IJobProcessorServices context)
        {
        }
        #endregion

        private byte[] StreamToByteArray(System.IO.Stream stream)
        {
            using (var memoryStream = new System.IO.MemoryStream())
            {
                stream.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
        }
    }
}