// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CacheAttribute.cs" company="Catel development team">
//   Copyright (c) 2008 - 2013 Catel development team. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Catel.Fody
{
    using System;

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class CacheAttribute : Attribute
    {
        public CacheAttribute(string keyPattern)
        {
        }
    }
}