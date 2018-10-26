using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Autodesk.Connectivity.WebServices;
using Autodesk.Connectivity.WebServicesTools;
using MFG124959.Classes.VaultInterface;
using VDF = Autodesk.DataManagement.Client.Framework;

namespace MFG124959.Classes.Vault2018
{
    public class VaultFunctions : IVaultFunctions
    {
        private VDF.Vault.Currency.Connections.Connection _connection;
        private WebServiceManager _wsm;
        private CustEntDef[] _custEntDefs;

        public void Initialize(object connection)
        {
            _connection = (VDF.Vault.Currency.Connections.Connection)connection;
            _wsm = _connection.WebServiceManager;
            _custEntDefs = _wsm.CustomEntityService.GetAllCustomEntityDefinitions();
        }

        public string GetCustomEntityValue(string custEntDefName)
        {
            var entity = SelectCustomEntity(custEntDefName);
            if (entity != null)
                return entity.EntityName;

            return null;
        }

        private VDF.Vault.Currency.Entities.IEntity SelectCustomEntity(string custEntDefName)
        {
            var custEntDef = _custEntDefs.SingleOrDefault(c => c.DispName.Equals(custEntDefName));
            if (custEntDef != null)
            {
                var entities = new List<VDF.Vault.Currency.Entities.IEntity>();
                var custEnts = GetAllCustomEntities(custEntDef);
                foreach (var custEnt in custEnts)
                    entities.Add(new VDF.Vault.Currency.Entities.CustomObject(
                        _connection, custEnt));

                var icon = GetCustomEntityIcon(custEntDef);
                var form = new EntityPickerDialog(_connection, entities, 
                    custEntDefName, icon, "Grid.EntityPicker.Classes");

                if (form.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    return form.Selection.ToArray()[0];
            }

            return null;
        }

        private Icon GetCustomEntityIcon(CustEntDef custEntDef)
        {
            var byteArray = _wsm.CustomEntityService.GetCustomEntityDefinitionIcons(
                new[] { custEntDef.Id })[0];

            using (System.IO.MemoryStream ms = new System.IO.MemoryStream(byteArray.Bytes))
            {
                return new Icon(ms);
            }
        }

        private IEnumerable<CustEnt> GetAllCustomEntities(CustEntDef custEntDef)
        {
            var custEnts = new List<CustEnt>();

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