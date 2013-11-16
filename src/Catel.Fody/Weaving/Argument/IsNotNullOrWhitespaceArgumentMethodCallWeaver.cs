﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IsNotNullOrWhitespaceArgumentMethodCallWeaver.cs" company="Catel development team">
//   Copyright (c) 2008 - 2013 Catel development team. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Catel.Fody.Weaving.Argument
{
    public sealed class IsNotNullOrWhitespaceArgumentMethodCallWeaver : ArgumentMethodCallWeaverBase
    {
        #region Constructors
        public IsNotNullOrWhitespaceArgumentMethodCallWeaver()
            : base("IsNotNullOrWhitespace")
        {
        }
        #endregion
    }
}