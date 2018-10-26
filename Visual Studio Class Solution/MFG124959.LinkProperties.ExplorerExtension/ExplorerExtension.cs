using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Windows.Forms;
using Autodesk.Connectivity.Explorer.Extensibility;
using Autodesk.Connectivity.WebServices;
using VDF = Autodesk.DataManagement.Client.Framework;

[assembly: Autodesk.Connectivity.Extensibility.Framework.ExtensionId("7ADC0766-F085-46d7-A2EB-C68F79CBF4E1")]
[assembly: Autodesk.Connectivity.Extensibility.Framework.ApiVersion("11.0")]

namespace MFG124959.LinkProperties.ExplorerExtension
{
    public class ExplorerExtension : IExplorerExtension
    {
        #region IExtension Members
        public IEnumerable<CommandSite> CommandSites()
        {
            var cmdItem = new CommandItem("CreateOrder.Command", "Create new Order...")
            {
                NavigationTypes = new[] { SelectionTypeId.FileVersion },
                MultiSelectEnabled = false
            };
            cmdItem.Execute += CreateOrderCommand;

            var cmdSite = new CommandSite("CreateOrder.FileContextMenu", "Order")
            {
                Location = CommandSiteLocation.FileContextMenu,
                DeployAsPulldownMenu = false
            };
            cmdSite.AddCommand(cmdItem);

            return new List<CommandSite> { cmdSite };
        }

        public IEnumerable<DetailPaneTab> DetailTabs()
        {
            // The json file only gets created, if Vault DataStandard is installed
            var jsonFile = @"C:\ProgramData\Autodesk\Vault 2018\Extensions\DataStandard\" + 
                @"Vault\CustomEntityDefinitions.json";
            if (System.IO.File.Exists(jsonFile))
            {
                var text = System.IO.File.ReadAllText(jsonFile);
                var definitions = Newtonsoft.Json.JsonConvert.
                    DeserializeObject<CustomEntityDefinition[]>(text);
                foreach (var definition in definitions)
                {
                    var entityDefinition = definition.EntityDefinitions.FirstOrDefault(
                        e => e.dispNameField == "Organisation");
                    if (entityDefinition != null)
                    {
                        var selectionTypeId = new SelectionTypeId(entityDefinition.nameField);
                        var detailPaneTab = new DetailPaneTab(
                            "Organisation.Tab.OrdersTab",
                            "Orders",
                            selectionTypeId,
                            typeof(OrdersUserControl));
                        detailPaneTab.SelectionChanged += OrganisationSelectionChanged;

                        return new List<DetailPaneTab> { detailPaneTab };
                    }
                }
            }

            return null;
        }

        public void OnLogOn(IApplication application)
        {
        }

        public void OnLogOff(IApplication application)
        {
        }

        public void OnShutdown(IApplication application)
        {
        }

        public void OnStartup(IApplication application)
        {
        }

        public IEnumerable<string> HiddenCommands()
        {
            return null;
        }

        public IEnumerable<CustomEntityHandler> CustomEntityHandlers()
        {
            return null;
        }
        #endregion

        private void CreateOrderCommand(object s, CommandItemEventArgs e)
        {
            var wsm = e.Context.Application.Connection.WebServiceManager;
            var file = wsm.DocumentService.GetFileById(e.Context.CurrentSelectionSet.First().Id);

            var dialog = new OrganisationPickerDialog(e.Context.Application.Connection);
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                var orderNumber = dialog.OrderNumber;
                var entity = dialog.Selection.FirstOrDefault();
                if (entity == null)
                    return;

                var link = wsm.DocumentService.AddLink(
                    entity.EntityIterationId, "FILE", file.Id, null);

                var propDefs = wsm.PropertyService.GetPropertyDefinitionsByEntityClassId("LINK");
                var propDefOrderNumber = propDefs.SingleOrDefault
                    (p => p.DispName == "Order Number");
                var propDefOrderFileId = propDefs.SingleOrDefault(
                    p => p.DispName == "Order File ID");
                if (propDefOrderNumber == null || propDefOrderFileId == null)
                    throw new ConfigurationErrorsException(
                        "The UDPs 'Order Number' and 'Order File ID' have to be present!");

                var paramOrderNumber = new PropInstParam
                {
                    PropDefId = propDefOrderNumber.Id,
                    Val = orderNumber
                };
                var paramOrderFileVersion = new PropInstParam
                {
                    PropDefId = propDefOrderFileId.Id,
                    Val = file.Id
                };
                var propInstParamArray = new PropInstParamArray
                {
                    Items = new[] { paramOrderNumber, paramOrderFileVersion }
                };

                wsm.DocumentServiceExtensions.UpdateLinkProperties(
                    new[] { link.Id }, new[] { propInstParamArray });              
            }
        }

        private void OrganisationSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Context.SelectedObject == null)
                return;

            var wsm = e.Context.Application.Connection.WebServiceManager;
            var custEnt = wsm.CustomEntityService.GetCustomEntitiesByIds(
                new [] { e.Context.SelectedObject.Id })[0];

            var propDefs = wsm.PropertyService.GetPropertyDefinitionsByEntityClassId("LINK");
            var propDefOrderNumber = propDefs.SingleOrDefault(p => p.DispName == "Order Number");
            var propDefOrderFileId = propDefs.SingleOrDefault(p => p.DispName == "Order File ID");
            if (propDefOrderNumber == null || propDefOrderFileId == null)
                throw new ConfigurationErrorsException(
                    "The UDPs 'Order Number' and 'Order File ID' have to be present!");

            var entities = new List<VDF.Vault.Currency.Entities.IEntity>();
            var propInsts = new List<PropInst>().ToArray();

            var links = wsm.DocumentService.GetLinksByParentIds(
                new[] { custEnt.Id }, new[] { "FILE" });
            if (links != null)
            {
                propInsts = wsm.PropertyService.GetProperties(
                    "LINK",
                    links.Select(l => l.Id).ToArray(),
                    new[] { propDefOrderNumber.Id, propDefOrderFileId.Id });

                foreach (var link in links)
                {
                    var propInstOrderFileId = propInsts.FirstOrDefault(
                        p => p.EntityId == link.Id && p.PropDefId == propDefOrderFileId.Id);
                    
                    if (propInstOrderFileId != null && propInstOrderFileId.Val != null)
                    {
                        var fileId = long.Parse(propInstOrderFileId.Val.ToString());
                        var file = wsm.DocumentService.GetFileById(fileId);
                        var entity = new VDF.Vault.Currency.Entities.FileIteration(
                            e.Context.Application.Connection, file);
                        
                        entities.Add(entity);
                    }
                }
            }

            var ordersUserControl = (OrdersUserControl)e.Context.UserControl;
            ordersUserControl.Reload(e.Context.Application.Connection, 
                entities, propDefOrderNumber.Id, propDefOrderFileId.Id, propInsts);
        }
    }
}