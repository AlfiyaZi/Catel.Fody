﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CatelTypeNode.cs" company="Catel development team">
//   Copyright (c) 2008 - 2013 Catel development team. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Catel.Fody
{
    using System.Collections.Generic;
    using System.Linq;
    using Mono.Cecil;

    public enum CatelTypeType
    {
        Model,

        ViewModel,

        Unknown
    }

    public class CatelType
    {
        public CatelType(TypeDefinition typeDefinition)
        {
            Nodes = new List<CatelType>();
            Mappings = new List<MemberMapping>();
            Properties = new List<CatelTypeProperty>();

            TypeDefinition = typeDefinition;

            DetermineCatelType();
            DetermineTypes();
            DetermineMethods();
            Properties = DetermineProperties();
            DetermineMappings();
        }

        public TypeDefinition TypeDefinition { get; private set; }
        public CatelTypeType Type { get; private set; }

        public List<CatelType> Nodes { get; private set; }

        public List<MemberMapping> Mappings { get; set; }

        public TypeReference PropertyDataType { get; private set; }

        public MethodReference RegisterPropertyInvoker { get; private set; }
        public MethodReference SetValueInvoker { get; private set; }
        public MethodReference GetValueInvoker { get; private set; }

        public List<CatelTypeProperty> Properties { get; private set; }

        private void DetermineCatelType()
        {
            if (TypeDefinition.ImplementsViewModelBase())
            {
                Type = CatelTypeType.ViewModel;
            }
            else if (TypeDefinition.ImplementsCatelModel())
            {
                Type = CatelTypeType.Model;
            }
            else
            {
                Type = CatelTypeType.Unknown;
            }
        }

        private void DetermineTypes()
        {
            var module = TypeDefinition.Module;

            PropertyDataType = module.Import(TypeDefinition.Module.FindType("Catel.Core", "PropertyData"));
        }

        private void DetermineMethods()
        {
            var module = TypeDefinition.Module;

            RegisterPropertyInvoker = module.Import(FindRegisterPropertyMethod(TypeDefinition));
            GetValueInvoker = module.Import(RecursiveFindMethod(TypeDefinition, "GetValue", true).GetGeneric());
            SetValueInvoker = module.Import(RecursiveFindMethod(TypeDefinition, "SetValue"));   
        }

        private List<CatelTypeProperty> DetermineProperties()
        {
            var properties = new List<CatelTypeProperty>();
            var typeProperties = TypeDefinition.Properties;

            foreach (var typeProperty in typeProperties)
            {
                if (typeProperty.IsDecoratedWithAttribute("NoWeavingAttribute"))
                {
                    typeProperty.RemoveAttribute("NoWeavingAttribute");
                    continue;
                }

                if (typeProperty.SetMethod == null)
                {
                    continue;
                }

                if (typeProperty.SetMethod.IsStatic)
                {
                    continue;
                }

                properties.Add(new CatelTypeProperty(TypeDefinition, typeProperty));
            }

            return properties;
        }

        private void DetermineMappings()
        {
            
        }

        private MethodReference FindRegisterPropertyMethod(TypeDefinition typeDefinition)
        {
            var typeDefinitions = new Stack<TypeDefinition>();
            MethodDefinition methodDefinition;
            var currentTypeDefinition = typeDefinition;

            do
            {
                typeDefinitions.Push(currentTypeDefinition);

                var methods = (from method in currentTypeDefinition.Methods
                               where method.Name == "RegisterProperty" && method.IsPublic
                               select method).ToList();

                if (methods.Count > 0)
                {
                    // We now we have to use the last one
                    methodDefinition = methods[methods.Count - 1];
                    break;
                }

                var baseType = currentTypeDefinition.BaseType;
                if (baseType == null || baseType.FullName == "System.Object")
                {
                    return null;
                }

                currentTypeDefinition = baseType.ResolveType();
            } while (true);

            return methodDefinition;
        }

        private MethodReference RecursiveFindMethod(TypeDefinition typeDefinition, string methodName, bool findGenericDefinition = false)
        {
            var typeDefinitions = new Stack<TypeDefinition>();
            MethodDefinition methodDefinition;
            var currentTypeDefinition = typeDefinition;

            do
            {
                typeDefinitions.Push(currentTypeDefinition);

                if (FindMethodDefinition(currentTypeDefinition, methodName, findGenericDefinition, out methodDefinition))
                {
                    break;
                }
                var baseType = currentTypeDefinition.BaseType;

                if (baseType == null || baseType.FullName == "System.Object")
                {
                    return null;
                }

                currentTypeDefinition = baseType.ResolveType();
            } while (true);

            return methodDefinition.GetMethodReference(typeDefinitions);
        }

        private bool FindMethodDefinition(TypeDefinition type, string methodName, bool findGenericDefinition, out MethodDefinition methodDefinition)
        {
            if (!findGenericDefinition)
            {
                methodDefinition = type.Methods
                                       .Where(x => x.Name == methodName)
                                       .OrderBy(definition => definition.Parameters.Count)
                                       .FirstOrDefault();
            }
            else
            {
                methodDefinition = (from method in type.Methods
                                    where method.Name == methodName &&
                                          method.HasGenericParameters
                                    select method).FirstOrDefault();
            }

            return methodDefinition != null;
        }

    }
}