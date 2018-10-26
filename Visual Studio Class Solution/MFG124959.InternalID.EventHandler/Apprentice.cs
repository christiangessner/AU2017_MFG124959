using System;
using Inventor;

namespace MFG124959.InternalID.EventHandler
{
    public class Apprentice
    {
        public class DocumentInfo
        {
            public string InternalName;
            public string Class;
        }

        public static DocumentInfo GetDocumentInfo(string fullFileName)
        {
            var documentInfo = new DocumentInfo();
            ApprenticeServerComponent apprentice = null;
            try
            {
                apprentice = new ApprenticeServerComponent();
                var document = apprentice.Open(fullFileName);

                documentInfo.InternalName = (string)document.GetType().InvokeMember(
                    "InternalName",
                    System.Reflection.BindingFlags.GetProperty, 
                    null, 
                    document, 
                    null);

                var propertySets = (PropertySets)document.GetType().InvokeMember(
                    "PropertySets", 
                    System.Reflection.BindingFlags.GetProperty, 
                    null, 
                    document, 
                    null);

                if (propertySets != null)
                {
                    foreach (PropertySet propertySet in propertySets)
                    {
                        if (propertySet.DisplayName == "User Defined Properties")
                        {
                            foreach (Property property in propertySet)
                            {
                                if (property.Name != "Class") continue;
                                documentInfo.Class = property.Value;
                                break;
                            }
                            break;
                        }
                    }                    
                }
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
            return documentInfo;
        }
    }
}