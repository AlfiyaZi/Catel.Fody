﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IsNotNullOrEmptyArrayArgumentMethodCallWeaver.cs" company="Catel development team">
//   Copyright (c) 2008 - 2013 Catel development team. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Catel.Fody.Weaving.Argument
{
    using System.Collections.Generic;
    using System.Linq;

    using Mono.Cecil;
    using Mono.Cecil.Cil;

    public sealed class IsNotNullOrEmptyArrayArgumentMethodCallWeaver : ArgumentMethodCallWeaverBase
    {
        protected override void BuildInstructions(TypeDefinition type, MethodDefinition methodDefinition, ParameterDefinition parameter, CustomAttribute attribute, List<Instruction> instructions)
        {
            instructions.Add(Instruction.Create(OpCodes.Ldstr, parameter.Name));
            instructions.Add(Instruction.Create(OpCodes.Ldarg_S, parameter));
        }

        protected override void SelectMethod(TypeDefinition argumentTypeDefinition, out MethodDefinition selectedMethod)
        {
            selectedMethod = argumentTypeDefinition.Methods.FirstOrDefault(definition => definition.Name == "IsNotNullOrEmptyArray" && definition.Parameters.Count == 2);
        }
    }
}