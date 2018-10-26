using System.Collections.Generic;
using System.Windows.Forms;
using Autodesk.Connectivity.WebServices;
using VDF = Autodesk.DataManagement.Client.Framework;

namespace MFG124959.LinkProperties.ExplorerExtension
{
    public partial class OrdersUserControl : UserControl
    {
        private VDF.Vault.Forms.Models.ViewVaultNavigationModel _navigationModel;

        public OrdersUserControl()
        {
            InitializeComponent();
        }

        public void Reload(VDF.Vault.Currency.Connections.Connection conn, 
            IEnumerable<VDF.Vault.Currency.Entities.IEntity> entities, 
            long propDefIdOrderNumber, long propDefIdOrderFileId, PropInst[] propInsts)
        {
            var configuration = new VDF.Vault.Forms.Controls.VaultBrowserControl.Configuration(
                conn, "Grid.OrganisationOrder.LinkProperties", null);

            configuration.AddInitialColumn("Name");
            configuration.AddInitialSortCriteria("Name", true);

            var orderPropertyExtensionProvider = new OrderPropertyExtensionProvider(
                propDefIdOrderNumber, 
                propDefIdOrderFileId, 
                propInsts);
            configuration.AddPropertyExtensionProvider(orderPropertyExtensionProvider);

            _navigationModel = new VDF.Vault.Forms.Models.ViewVaultNavigationModel();
            _navigationModel.AddContent(entities);

            vaultBrowserControl1.SetDataSource(configuration, _navigationModel);
        }
    }
}