using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Autodesk.Connectivity.WebServices;
using Autodesk.Connectivity.WebServicesTools;
using VDF = Autodesk.DataManagement.Client.Framework;

namespace MFG124959.LinkProperties.ExplorerExtension
{
    public partial class OrganisationPickerDialog : Form
    {
        private readonly VDF.Vault.Forms.Models.ViewVaultNavigationModel _navigationModel;
        private readonly WebServiceManager _wsm;

        public OrganisationPickerDialog(VDF.Vault.Currency.Connections.Connection connection)
        {
            InitializeComponent();

            _wsm = connection.WebServiceManager;
            var custEntDefs = _wsm.CustomEntityService.GetAllCustomEntityDefinitions();
            var custEntDef = custEntDefs.SingleOrDefault(c => c.DispName.Equals("Organisation"));

            Icon = GetCustomEntityIcon(custEntDef);
            var entities = new List<VDF.Vault.Currency.Entities.IEntity>();
            var custEnts = GetAllCustomEntities(custEntDef);
            foreach (var custEnt in custEnts)
            {
                entities.Add(new VDF.Vault.Currency.Entities.CustomObject(connection, custEnt));
            }
            var configuration = new VDF.Vault.Forms.Controls.VaultBrowserControl.Configuration(
                connection, 
                "Grid.SelectOrganisationAndOrderNumber", 
                null);

            configuration.AddInitialColumn("Name");
            configuration.AddInitialSortCriteria("Name", true);

            _navigationModel = new VDF.Vault.Forms.Models.ViewVaultNavigationModel();
            _navigationModel.AddContent(entities);

            vaultBrowserControl1.SetDataSource(configuration, _navigationModel);
            vaultBrowserControl1.Refresh();
        }

        public IEnumerable<VDF.Vault.Currency.Entities.IEntity> Selection
        {
            get { return _navigationModel.SelectedContent; }
        }

        public string OrderNumber
        {
            get { return txtOrderNumber.Text; }
        }

        private void buttonOk_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        public Icon GetCustomEntityIcon(CustEntDef custEntDef)
        {
            var byteArray = _wsm.CustomEntityService.GetCustomEntityDefinitionIcons(
                new[] { custEntDef.Id })[0];
            using (System.IO.MemoryStream ms = new System.IO.MemoryStream(byteArray.Bytes))
            {
                return new Icon(ms);
            }
        }

        public IEnumerable<CustEnt> GetAllCustomEntities(CustEntDef custEntDef)
        {
            var propDefs = _wsm.PropertyService.GetPropertyDefinitionsByEntityClassId("CUSTENT");
            var propDef = propDefs.Single(p => p.SysName.Equals("CustomEntitySystemName"));

            var srcCond = new SrchCond
            {
                PropDefId = propDef.Id,
                PropTyp = PropertySearchType.SingleProperty,
                SrchOper = 3,
                SrchRule = SearchRuleType.Must,
                SrchTxt = custEntDef.Name
            };

            string bookmark = null;
            SrchStatus status = null;
            var custEnts = new List<CustEnt>();

            while (status == null || custEnts.Count < status.TotalHits)
            {
                var results = _wsm.CustomEntityService.FindCustomEntitiesBySearchConditions(
                    new[] { srcCond }, null, ref bookmark, out status);

                if (results != null)
                    custEnts.AddRange(results);
                else
                    break;
            }

            return custEnts;
        }
    }
}