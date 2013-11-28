// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CacheWeaver.cs" company="Catel development team">
//   Copyright (c) 2008 - 2013 Catel development team. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Catel.Fody.Weaving.Cache
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    using Mono.Cecil;
    using Mono.Cecil.Cil;
    using Mono.Cecil.Rocks;
    using Mono.Collections.Generic;

    public class CacheWeaver
    {
        #region Fields
        private readonly TypeDefinition _type;
        #endregion

        #region Constructors
        public CacheWeaver(TypeDefinition type)
        {
            _type = type;
        }
        #endregion

        #region Methods
        public void Execute()
        {
            List<MethodDefinition> methodDefinitions = _type.Methods.ToList();
            foreach (var originalMethodDefinition in methodDefinitions)
            {
                CustomAttribute cacheAttribute = originalMethodDefinition.GetAttribute("Catel.Fody.Cache");
                if (cacheAttribute != null)
                {
                    // TODO: Compute the unique name for field and shalow method copy
                    string uniqueName = "_____" + originalMethodDefinition.Name;

                    CreateACopyOfMethodAs(uniqueName, originalMethodDefinition);
                    CreateCacheStorageField(string.Format(CultureInfo.InvariantCulture, "{0}CacheStorage", uniqueName), FodyEnvironment.ModuleDefinition.FindType("mscorlib", "System.String"), originalMethodDefinition.ReturnType);

                    originalMethodDefinition.Body.Instructions.Clear();

                    /*
                        IL_0000: newobj instance void Catel.Fody.TestAssembly.CacheClass/'<>c__DisplayClass1'::.ctor()
                        IL_0005: stloc.0
                        IL_0006: ldloc.0
                        IL_0007: ldarg.1
                        IL_0008: stfld string Catel.Fody.TestAssembly.CacheClass/'<>c__DisplayClass1'::key
                        IL_000d: ldloc.0
                        IL_000e: ldarg.0
                        IL_000f: stfld class Catel.Fody.TestAssembly.CacheClass Catel.Fody.TestAssembly.CacheClass/'<>c__DisplayClass1'::'<>4__this'
                        IL_0014: nop
                        IL_0015: ldarg.0
                        IL_0016: ldfld class [Catel.Core]Catel.Caching.CacheStorage`2<string, string> Catel.Fody.TestAssembly.CacheClass::_getSomething2
                        IL_001b: ldstr "{0}"
                        IL_0020: ldloc.0
                        IL_0021: ldfld string Catel.Fody.TestAssembly.CacheClass/'<>c__DisplayClass1'::key
                        IL_0026: call string [mscorlib]System.String::Format(string,  object)
                        IL_002b: ldloc.0
                        IL_002c: ldftn instance string Catel.Fody.TestAssembly.CacheClass/'<>c__DisplayClass1'::'<GetSomething2>b__0'()
                        IL_0032: newobj instance void class [mscorlib]System.Func`1<string>::.ctor(object,  native int)
                        IL_0037: ldnull
                        IL_0038: ldc.i4.0
                        IL_0039: callvirt instance string class [Catel.Core]Catel.Caching.CacheStorage`2<string, string>::GetFromCacheOrFetch(!0,  class [mscorlib]System.Func`1<!1>,  class [Catel.Core]Catel.Caching.Policies.ExpirationPolicy,  bool)
                        IL_003e: stloc.1
                        IL_003f: br.s IL_0041
                        IL_0041: ldloc.1
                        IL_0042: ret
                    */
                }

                originalMethodDefinition.RemoveAttribute("Catel.Fody.Cache");
            }
        }

        private void CreateCacheStorageField(string uniqueName, TypeDefinition keyType, TypeReference resultType)
        {
            ModuleDefinition moduleDefinition = FodyEnvironment.ModuleDefinition;
            var cacheStorageGenericInstanteType = moduleDefinition.FindType("Catel.Core", "Catel.Caching.CacheStorage`2").MakeGenericInstanceType(keyType, resultType);
            var cacheStorageGenericConstructor = cacheStorageGenericInstanteType.Resolve().GetConstructors().FirstOrDefault().MakeGeneric(cacheStorageGenericInstanteType);

            TypeReference importedType = _type.Module.Import(cacheStorageGenericInstanteType);
            var fieldDefinition = new FieldDefinition(uniqueName, FieldAttributes.Private | FieldAttributes.InitOnly, importedType);

            _type.Fields.Add(fieldDefinition);

            if (!_type.GetConstructors().Any())
            {
                var defaultConstructor = new MethodDefinition(".cctor", MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, _type.Module.Import(typeof(void)));
                _type.Methods.Add(defaultConstructor);
                
                // TODO: Make the base call. 
                /*
                 IL_000e: call instance void [mscorlib]System.Object::.ctor()
                */
            }

            /*
                IL_0000: ldarg.0
                IL_0001: ldnull
                IL_0002: ldc.i4.0
                IL_0003: newobj instance void class [Catel.Core]Catel.Caching.CacheStorage`2<string, string>::.ctor(class [mscorlib]System.Func`1<class [Catel.Core]Catel.Caching.Policies.ExpirationPolicy>,  bool)
                IL_0008: stfld class [Catel.Core]Catel.Caching.CacheStorage`2<string, string> Catel.Fody.TestAssembly.CacheClass::_getSomething2
                IL_000d: ldarg.0
           */
            
            foreach (MethodDefinition methodDefinition in this._type.GetConstructors())
            {
                methodDefinition.Body.SimplifyMacros();
                
                methodDefinition.Body.Instructions.Insert(0, new List<Instruction>
                                                                 {
                                                                     Instruction.Create(OpCodes.Ldarg_0), 
                                                                     Instruction.Create(OpCodes.Ldnull), 
                                                                     Instruction.Create(OpCodes.Ldc_I4_0), 
                                                                     Instruction.Create(OpCodes.Newobj, _type.Module.Import(cacheStorageGenericConstructor)), 
                                                                     Instruction.Create(OpCodes.Stfld, fieldDefinition), 
                                                                     // Instruction.Create(OpCodes.Ldarg_0), 
                                                                 });
                methodDefinition.Body.OptimizeMacros();
            }
        }

        private void CreateACopyOfMethodAs(string uniqueName, MethodDefinition originalMethodDefinition)
        {
            var shallowMethodDefinition = new MethodDefinition(uniqueName, originalMethodDefinition.Attributes, originalMethodDefinition.ReturnType);

            if (originalMethodDefinition.HasParameters)
            {
                foreach (var parameter in originalMethodDefinition.Parameters)
                {
                    shallowMethodDefinition.Parameters.Add(parameter);
                }
            }

            if (originalMethodDefinition.HasBody)
            {
                if (originalMethodDefinition.Body.HasVariables)
                {
                    foreach (var instruction in originalMethodDefinition.Body.Variables)
                    {
                        shallowMethodDefinition.Body.Variables.Add(instruction);
                    }
                }

                if (originalMethodDefinition.Body.HasExceptionHandlers)
                {
                    foreach (var exceptionHandler in originalMethodDefinition.Body.ExceptionHandlers)
                    {
                        shallowMethodDefinition.Body.ExceptionHandlers.Add(exceptionHandler);
                    }
                }

                foreach (var instruction in originalMethodDefinition.Body.Instructions)
                {
                    shallowMethodDefinition.Body.Instructions.Add(instruction);
                }
            }

            shallowMethodDefinition.IsPrivate = true;
            _type.Methods.Add(shallowMethodDefinition);
        }
        #endregion
    }
}