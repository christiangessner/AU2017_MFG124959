namespace MFG124959.Classes.VaultInterface
{
    public interface IVaultFunctions
    {
        void Initialize(object connection);
        string GetCustomEntityValue(string custEntDefName);
    }
}