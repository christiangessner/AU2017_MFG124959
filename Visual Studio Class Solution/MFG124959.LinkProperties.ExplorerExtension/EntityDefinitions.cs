namespace MFG124959.LinkProperties.ExplorerExtension
{
    public class CustomEntityDefinition
    {
        public string Server;
        public string Vault;
        public EntityDefinition[] EntityDefinitions;
    }

    public class EntityDefinition
    {
        public string dispNameField;
        public string dispNamePluralField;
        public int idField;
        public string nameField;
    }
}
