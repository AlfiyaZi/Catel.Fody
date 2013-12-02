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
            var module = _type.Module;
            var stringType = module.FindType("mscorlib", "System.String");
            var funcType = module.FindType("mscorlib", "System.Func`1");

            var methodDefinitions = _type.Methods.ToList();
            foreach (var methodDefinition in methodDefinitions)
            {
                CustomAttribute cacheAttribute = methodDefinition.GetAttribute("Catel.Fody.Cache");
                if (cacheAttribute != null)
                {
                    // TODO: Compute the unique name for field and shallow method copy
                    string uniqueName = "_____" + methodDefinition.Name;

                    var clonedMethodDefinition = CloneMethod(uniqueName, methodDefinition);
                    var cacheStorageField = CreateCacheStorageField(string.Format(CultureInfo.InvariantCulture, "{0}CacheStorage", uniqueName), FodyEnvironment.ModuleDefinition.FindType("mscorlib", "System.String"), methodDefinition.ReturnType);

                    var methodBody = methodDefinition.Body;
                    var instructions = methodBody.Instructions;
                    instructions.Clear();

                    /*
                        string str = string.Format("{0}", key);
                        Func<string> code = () => this.____GetFromCache2(key);
                        return ____GetFromCache2CacheStorage.GetFromCacheOrFetch(str, code, null, false);
                     
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

                    var keyNameVariable = new VariableDefinition("keyNameVariable", stringType);
                    methodBody.Variables.Add(keyNameVariable);

                    var genericFuncType = funcType.MakeGenericInstanceType(methodDefinition.ReturnType);
 
                    var addToCacheFunctionVariable = new VariableDefinition("addToCacheFunction", module.Import(genericFuncType));
                    methodBody.Variables.Add(addToCacheFunctionVariable);

                    // string str = string.Format("{0}", key);
                    var formattingString = (string)cacheAttribute.ConstructorArguments[0].Value;
                    instructions.Add(Instruction.Create(OpCodes.Ldstr, formattingString));

                    foreach (var parameter in methodDefinition.Parameters)
                    {
                        instructions.Add(Instruction.Create(OpCodes.Ldarg, parameter));
                    }

                    instructions.Add(Instruction.Create(OpCodes.Ldloc, keyNameVariable));

                    var formatMethod = module.Import((from method in stringType.Methods
                                                      where string.Equals(method.Name, "Format") &&
                                                            method.Parameters.Count == 2 &&
                                                            method.Parameters[1].ParameterType.IsArray
                                                      select method).First());

                    instructions.Add(Instruction.Create(OpCodes.Call, formatMethod));

                    // Func<string> code = () => this.____GetFromCache2(key);
                    instructions.Add(Instruction.Create(OpCodes.Ldloc, addToCacheFunctionVariable));

                    // TODO: Create display class, etc


                    // return ____GetFromCache2CacheStorage.GetFromCacheOrFetch(str, code, null, false);
                    // TODO: Load variables

                    var getCacheStorageMethod = module.Import((from method in cacheStorageField.FieldType.Resolve().Methods
                                                               where string.Equals(method.Name, "GetFromCacheOrFetch")
                                                               select method).First());
                    instructions.Add(Instruction.Create(OpCodes.Callvirt, getCacheStorageMethod));

                    methodBody.OptimizeMacros();
                }

                methodDefinition.RemoveAttribute("Catel.Fody.Cache");
            }
        }

        private FieldDefinition CreateCacheStorageField(string uniqueName, TypeDefinition keyType, TypeReference resultType)
        {
            var moduleDefinition = FodyEnvironment.ModuleDefinition;
            var cacheStorageGenericInstanteType = moduleDefinition.FindType("Catel.Core", "Catel.Caching.CacheStorage`2").MakeGenericInstanceType(keyType, resultType);
            var cacheStorageGenericConstructor = cacheStorageGenericInstanteType.Resolve().GetConstructors().FirstOrDefault().MakeGeneric(cacheStorageGenericInstanteType);

            var importedType = _type.Module.Import(cacheStorageGenericInstanteType);
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

            foreach (var methodDefinition in _type.GetConstructors())
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

            return fieldDefinition;
        }

        private MethodDefinition CloneMethod(string uniqueName, MethodDefinition originalMethodDefinition)
        {
            var clonedMethodDefinition = new MethodDefinition(uniqueName, originalMethodDefinition.Attributes, originalMethodDefinition.ReturnType);

            if (originalMethodDefinition.HasParameters)
            {
                foreach (var parameter in originalMethodDefinition.Parameters)
                {
                    clonedMethodDefinition.Parameters.Add(parameter);
                }
            }

            if (originalMethodDefinition.HasBody)
            {
                if (originalMethodDefinition.Body.HasVariables)
                {
                    foreach (var instruction in originalMethodDefinition.Body.Variables)
                    {
                        clonedMethodDefinition.Body.Variables.Add(instruction);
                    }
                }

                if (originalMethodDefinition.Body.HasExceptionHandlers)
                {
                    foreach (var exceptionHandler in originalMethodDefinition.Body.ExceptionHandlers)
                    {
                        clonedMethodDefinition.Body.ExceptionHandlers.Add(exceptionHandler);
                    }
                }

                foreach (var instruction in originalMethodDefinition.Body.Instructions)
                {
                    clonedMethodDefinition.Body.Instructions.Add(instruction);
                }
            }

            clonedMethodDefinition.IsPrivate = true;

            var compilerGeneratedAttribute = originalMethodDefinition.Module.FindType("mscorlib", "System.Runtime.CompilerServices.CompilerGeneratedAttribute");
            clonedMethodDefinition.CustomAttributes.Add(new CustomAttribute(originalMethodDefinition.DeclaringType.Module.Import(compilerGeneratedAttribute.Resolve().Constructor(false))));

            _type.Methods.Add(clonedMethodDefinition);

            return clonedMethodDefinition;
        }
        #endregion
    }
}