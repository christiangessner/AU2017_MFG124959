using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using VDF = Autodesk.DataManagement.Client.Framework;

namespace MFG124959.Classes.Vault2018
{
    public partial class EntityPickerDialog : Form
    {
        private readonly VDF.Vault.Forms.Models.ViewVaultNavigationModel _navigationModel;

        public EntityPickerDialog(VDF.Vault.Currency.Connections.Connection conn, 
            IEnumerable<VDF.Vault.Currency.Entities.IEntity> entities, string title, Icon icon, 
            string persistenceKey)
        {
            InitializeComponent();

            Text = title;
            if (icon != null)
                Icon = icon;

            var configuration = new VDF.Vault.Forms.Controls.VaultBrowserControl.Configuration(
                conn, persistenceKey, null);
            configuration.AddInitialColumn("Name");
            configuration.AddInitialSortCriteria("Name", true);

            _navigationModel = new VDF.Vault.Forms.Models.ViewVaultNavigationModel();
            _navigationModel.AddContent(entities);

            vaultBrowserControl1.SetDataSource(configuration, _navigationModel);
        }

        public sealed override string Text
        {
            get { return base.Text; }
            set { base.Text = value; }
        }

        public IEnumerable<VDF.Vault.Currency.Entities.IEntity> Selection
        {
            get { return _navigationModel.SelectedContent; }
        }

        private void buttonOk_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}