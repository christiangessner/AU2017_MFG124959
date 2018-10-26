using System;
using Inventor;

namespace MFG124959.InternalID.Job
{
    public class Apprentice
    {
        public static string GetInternalId(string fullFileName)
        {
            string internalId = null;
            ApprenticeServerComponent apprentice = null;
            try
            {
                apprentice = new ApprenticeServerComponent();
                var document = apprentice.Open(fullFileName);

                internalId = (string)document.GetType().InvokeMember(
                    "InternalName", 
                    System.Reflection.BindingFlags.GetProperty, 
                    null, 
                    document, 
                    null);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                if (apprentice != null)
                    apprentice.Close();
            }
            return internalId;
        }
    }
}