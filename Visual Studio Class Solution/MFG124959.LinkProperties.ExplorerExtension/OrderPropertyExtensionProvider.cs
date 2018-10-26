using System.Collections.Generic;
using System.Linq;
using Autodesk.Connectivity.WebServices;
using Autodesk.DataManagement.Client.Framework.Vault.Currency.Connections;
using Autodesk.DataManagement.Client.Framework.Vault.Currency.Entities;
using Autodesk.DataManagement.Client.Framework.Vault.Currency.Properties;
using Autodesk.DataManagement.Client.Framework.Vault.Interfaces;

namespace MFG124959.LinkProperties.ExplorerExtension
{
    public class OrderPropertyExtensionProvider : IPropertyExtensionProvider
    {
        private readonly PropertyDefinition _orderPropertyDefinition;
        private readonly long _propDefIdOrderNumber;
        private readonly long _propDefIdOrderFileId;
        private readonly PropInst[] _propInsts;

        public OrderPropertyExtensionProvider(long propDefIdOrderNumber, long propDefIdOrderFileId, PropInst[] propInsts)
        {
            _orderPropertyDefinition = new PropertyDefinition("Order Number");
            _propDefIdOrderNumber = propDefIdOrderNumber;
            _propDefIdOrderFileId = propDefIdOrderFileId;
            _propInsts = propInsts;
        }

        public void DecoratePropertyDefinitions(Connection connection, PropertyDefinitionDictionary existingPropDefs)
        {
        }

        public void PostGetPropertyValues(Connection connection, IEnumerable<IEntity> entities, PropertyDefinitionDictionary propDefs, PropertyValues resultValues, PropertyValueSettings settings)
        {
        }

        public IEnumerable<PropertyDefinition> GetCustomPropertyDefinitions(Connection connection, PropertyDefinitionDictionary existingPropDefs)
        {
            return new[] { _orderPropertyDefinition };
        }

        public void PreGetPropertyValues(Connection connection, IEnumerable<IEntity> entities, PropertyDefinitionDictionary propDefs, PropertyValues resultValues, PropertyValueSettings settings)
        {
            if (_propInsts == null)
                return;

            foreach (var entity in entities)
            {
                var propInstOrderFileId = _propInsts.FirstOrDefault(p => p.Val != null && p.Val.ToString() == entity.EntityIterationId.ToString() && 
                    p.PropDefId == _propDefIdOrderFileId);

                if (propInstOrderFileId != null)
                {
                    var propInstOrderNumber = _propInsts.FirstOrDefault(p => p.EntityId == propInstOrderFileId.EntityId && 
                        p.PropDefId == _propDefIdOrderNumber);
                    if (propInstOrderNumber != null)
                        resultValues.SetValue(new PropertyValue(entity, _orderPropertyDefinition, propInstOrderNumber.Val));
                }
            }
        }

        public string[] SupportedEntityClasses
        {
            get { return null; }
        }
    }
}