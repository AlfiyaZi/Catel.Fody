// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ArgumentInstructionSequenceBuilder.cs" company="Catel development team">
//   Copyright (c) 2008 - 2013 Catel development team. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Catel.Fody.Weaving.Argument
{
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    using Mono.Cecil;
    using Mono.Cecil.Cil;

    internal static class ArgumentInstructionSequenceBuilder
    {
        #region Methods
        public static IEnumerable<Instruction> BuildIsMatchOrIsNotMatchInstructions(ParameterDefinition parameter, CustomAttribute attribute)
        {
            var pattern = (string)attribute.ConstructorArguments[0].Value;
            var regexOptions = (RegexOptions)attribute.ConstructorArguments[1].Value;

            yield return Instruction.Create(OpCodes.Ldstr, parameter.Name);
            yield return Instruction.Create(OpCodes.Ldarg_S, parameter);
            yield return Instruction.Create(OpCodes.Ldstr, pattern);
            yield return Instruction.Create(OpCodes.Ldc_I4, (int)regexOptions);
        }

        public static IEnumerable<Instruction> BuildDefaultInstructions(ParameterDefinition parameter)
        {
            yield return Instruction.Create(OpCodes.Ldstr, parameter.Name);
            yield return Instruction.Create(OpCodes.Ldarg_S, parameter);
        }

        public static IEnumerable<Instruction> BuildIsOfTypeOrImplementsInterfaceInstructions(ParameterDefinition parameter, CustomAttribute attribute)
        {
            yield return Instruction.Create(OpCodes.Ldstr, parameter.Name);
            yield return Instruction.Create(OpCodes.Ldarg_S, parameter);
            yield return Instruction.Create(OpCodes.Ldtoken, (TypeReference)attribute.ConstructorArguments[0].Value);
        }
        #endregion
    }
}