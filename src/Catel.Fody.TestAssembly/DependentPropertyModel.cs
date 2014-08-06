﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DependentPropertyModel.cs" company="Catel development team">
//   Copyright (c) 2008 - 2014 Catel development team. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


namespace Catel.Fody.TestAssembly
{
    using System.Collections.Generic;
    using Data;

    public class DependentPropertyModel : ModelBase
    {
        #region Properties
        public string FirstName { get; set; }

        // Using full property to check if these are supported as well
        public string MiddleName
        {
            get { return GetValue<string>(MiddleNameProperty); }
            set { SetValue(MiddleNameProperty, value); }
        }

        public static readonly PropertyData MiddleNameProperty = RegisterProperty("MiddleName", typeof(string), null);

        public string LastName { get; set; }

        public int Age { get; set; }

        public string FullName
        {
            get
            {
                var items = new List<string>();

                if (!string.IsNullOrWhiteSpace(FirstName))
                {
                    items.Add(FirstName);
                }

                if (!string.IsNullOrWhiteSpace(MiddleName))
                {
                    items.Add(MiddleName);
                }

                if (!string.IsNullOrWhiteSpace(LastName))
                {
                    items.Add(LastName);
                }

                return string.Join(" ", items);
            }
        }
        #endregion
    }

    public class DetailedDependentPropertyModel : DependentPropertyModel
    {
        #region Properties
        public string Profile
        {
            get { return string.Format("Name:{0}, Age:{1}", FullName, Age).Trim(); }
        }
        #endregion
    }
}