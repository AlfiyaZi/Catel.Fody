// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CacheWeaverService.cs" company="Catel development team">
//   Copyright (c) 2008 - 2013 Catel development team. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Catel.Fody.Services
{
    using System.Collections.Generic;

    using Catel.Fody.Weaving.Cache;

    using Mono.Cecil;

    public class CacheWeaverService
    {
        #region Fields
        private readonly List<TypeDefinition> _types;
        #endregion

        #region Constructors
        public CacheWeaverService(List<TypeDefinition> types)
        {
            _types = types;
        }
        #endregion

        public void Execute()
        {
            foreach (var typeDefinition in _types)
            {
                new CacheWeaver(typeDefinition).Execute();
            }
        }
    }
}