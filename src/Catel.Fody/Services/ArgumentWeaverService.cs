﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ArgumentWeaverService.cs" company="Catel development team">
//   Copyright (c) 2008 - 2013 Catel development team. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


namespace Catel.Fody.Services
{
    using System.Collections.Generic;
    using Weaving.Argument;
    using Mono.Cecil;

    public class ArgumentWeaverService
    {
        private readonly List<TypeDefinition> _allTypes;

        #region Constructors
        public ArgumentWeaverService(List<TypeDefinition> allTypes)
        {
            _allTypes = allTypes;
        }
        #endregion

        #region Methods
        public void Execute()
        {
            foreach (var type in _allTypes)
            {
                var weaver = new ArgumentWeaver(type);
                weaver.Execute();
            }
        }
        #endregion
    }
}