// http://help.autodesk.com/view/INVNTOR/2018/ENU/?guid=GUID-F473AB33-3A7A-4419-B903-14B61A745080

using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Inventor;
using Application = Inventor.Application;

namespace MFG124959.Classes.InventorAddIn
{
    [ProgId("InventorAddIn.StandardAddInServer"), Guid("b00d2a02-2c9d-44b6-975f-bc53fae66021")]
    public class AddInServer : ApplicationAddInServer
    {
        #region Private Members
        private Application _application;
        private ButtonDefinition _buttonDefinition;
        private UserInterfaceEvents _userInterfaceEvents;

        private string _clientId = "{b00d2a02-2c9d-44b6-975f-bc53fae66021}";

        private string _buttonDisplayName = "Assign Class";
        private string _buttonInternalName = "MFG124959.AssignClass.Button";
        private string _buttonDescription = "Select a 'Class' from Vault and write the class name to an iProperty";
        private string _buttonTooltip = "Select a 'Class' from Vault";

        private void CreateUserInterface()
        {
            var userInterfaceManager = _application.UserInterfaceManager;

            foreach (var ribbonName in new[] {"Part", "Assembly", "Drawing", "Presentation"})
            {
                var ribbon = userInterfaceManager.Ribbons[ribbonName];

                var ribbonTabLoggedOut = ribbon.RibbonTabs["id_TabTools"];
                var ribbonPanelLoggedOut = ribbonTabLoggedOut.RibbonPanels["id_PanelP_ToolsOptions"];
                ribbonPanelLoggedOut.CommandControls.AddButton(_buttonDefinition, true);
            }
        }

        private void SetInventorProperty(Document document, string propertySetName, string propertyName, object propertyValue)
        {
            try
            {
                var property = document.PropertySets[propertySetName][propertyName];
                property.Value = propertyValue;
            }
            catch
            {
                try
                {
                    document.PropertySets[propertySetName].Add(propertyValue, propertyName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }
        #endregion
        #region ApplicationAddInServer Members
        public void Activate(ApplicationAddInSite addInSiteObject, bool firstTime)
        {
            _application = addInSiteObject.Application;
            var controlDefs = _application.CommandManager.ControlDefinitions;
            var smallPicture = PictureConverter.ImageToPictureDisp(Properties.Resources.Autodesk.ToBitmap());
            var largePicture = PictureConverter.ImageToPictureDisp(Properties.Resources.Autodesk.ToBitmap());
            _buttonDefinition = controlDefs.AddButtonDefinition(_buttonDisplayName, _buttonInternalName,
                CommandTypesEnum.kNonShapeEditCmdType, _clientId, _buttonDescription, _buttonTooltip,
                smallPicture, largePicture);
            _buttonDefinition.OnExecute += ButtonOnExecute;

            if (firstTime)
                CreateUserInterface();

            _userInterfaceEvents = _application.UserInterfaceManager.UserInterfaceEvents;
            _userInterfaceEvents.OnResetRibbonInterface += context => CreateUserInterface();
        }
        public void Deactivate() { }
        public void ExecuteCommand(int commandId) { }
        public object Automation { get { return null; } }
        #endregion

        private void ButtonOnExecute(NameValueMap context)
        {
            var document = _application.ActiveDocument;

            var connection = VaultConnection.GetVaultConnection(_application);
            if (connection != null)
            {
                var assemblyLoader = new AssemblyLoader();
                var vaultFunctions = assemblyLoader.LoadLateBindingAssemblies();
                vaultFunctions.Initialize(connection);
                var name = vaultFunctions.GetCustomEntityValue("Class");
                if (name != null)
                {
                    SetInventorProperty(document, "User Defined Properties", "Class", name);
                    MessageBox.Show(
                        string.Format("iProperty 'Class' has been set to '{0}'", name), 
                        @"Assign Class");
                }
            }
        }
    }
}