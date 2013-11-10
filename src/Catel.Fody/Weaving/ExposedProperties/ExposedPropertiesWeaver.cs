﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ExposedPropertiesWeaver.cs" company="Catel development team">
//   Copyright (c) 2008 - 2013 Catel development team. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


namespace Catel.Fody.Weaving.ExposedProperties
{
    using System.Linq;
    using Mono.Cecil;

    public class ExposedPropertiesWeaver
    {
        private readonly CatelTypeNodeBuilder _catelTypeNodeBuilder;
        private static readonly TypeDefinition ViewModelToModelAttributeTypeDefinition;

        static ExposedPropertiesWeaver()
        {
            ViewModelToModelAttributeTypeDefinition = FodyEnvironment.ModuleDefinition.FindType("Catel.MVVM", "Catel.MVVM.ViewModelToModelAttribute") as TypeDefinition;
        }

        public ExposedPropertiesWeaver(CatelTypeNodeBuilder catelTypeNodeBuilder)
        {
            _catelTypeNodeBuilder = catelTypeNodeBuilder;
        }

        public void Execute()
        {
            if (ViewModelToModelAttributeTypeDefinition == null)
            {
                return;
            }

            foreach (var catelType in _catelTypeNodeBuilder.CatelTypes)
            {
                if (catelType.TypeDefinition.ImplementsViewModelBase())
                {
                    ProcessType(catelType);
                }
            }
        }

        private void ProcessType(CatelType catelType)
        {
            foreach (var property in catelType.Properties)
            {
                var propertyDefinition = property.PropertyDefinition;
                var exposeAttributes = propertyDefinition.GetAttributes("Catel.Fody.ExposeAttribute");
                foreach (var exposeAttribute in exposeAttributes)
                {
                    ProcessProperty(catelType, property, exposeAttribute);
                }

                propertyDefinition.RemoveAttribute("Catel.Fody.ExposeAttribute");
            }
        }

        private void ProcessProperty(CatelType catelType, CatelTypeProperty modelProperty, CustomAttribute exposeAttribute)
        {
            var modelName = modelProperty.Name;
            var viewModelPropertyName = (string)exposeAttribute.ConstructorArguments[0].Value;
            var modelPropertyName = (string)(exposeAttribute.ConstructorArguments[1].Value ?? viewModelPropertyName);

            // Check property definition on model
            var modelType = modelProperty.PropertyDefinition.PropertyType;
            var modelPropertyToMap = (from property in modelType.Resolve().Properties
                                      where string.Equals(modelPropertyName, property.Name)
                                      select property).FirstOrDefault();

            if (modelPropertyToMap == null)
            {
                FodyEnvironment.LogError(string.Format("Exposed property '{0}' does not exist on model '{1}', make sure to set the right mapping", modelPropertyName, modelType.FullName));
                return;
            }

            var modelPropertyType = modelPropertyToMap.PropertyType;

            var viewModelPropertyDefinition = new PropertyDefinition(viewModelPropertyName, PropertyAttributes.None, modelPropertyType);
            viewModelPropertyDefinition.DeclaringType = catelType.TypeDefinition;

            var compilerGeneratedAttribute = catelType.TypeDefinition.Module.FindType("mscorlib", "System.Runtime.CompilerServices.CompilerGeneratedAttribute");
            viewModelPropertyDefinition.CustomAttributes.Add(new CustomAttribute(catelType.TypeDefinition.Module.Import(compilerGeneratedAttribute.Resolve().Constructor(false))));

            catelType.TypeDefinition.Properties.Add(viewModelPropertyDefinition);
            var catelTypeProperty = new CatelTypeProperty(catelType.TypeDefinition, viewModelPropertyDefinition);

            var catelPropertyWeaver = new CatelPropertyWeaver(catelType, catelTypeProperty);
            catelPropertyWeaver.Execute(true);

            var stringTypeDefinition = catelType.TypeDefinition.Module.Import(typeof (string));

            var attributeConstructor = catelType.TypeDefinition.Module.Import(ViewModelToModelAttributeTypeDefinition.Constructor(false));
            var viewModelToModelAttribute = new CustomAttribute(attributeConstructor);
            viewModelToModelAttribute.ConstructorArguments.Add(new CustomAttributeArgument(stringTypeDefinition, modelName));
            viewModelToModelAttribute.ConstructorArguments.Add(new CustomAttributeArgument(stringTypeDefinition, modelPropertyName));
            viewModelPropertyDefinition.CustomAttributes.Add(viewModelToModelAttribute);
        }
    }
}