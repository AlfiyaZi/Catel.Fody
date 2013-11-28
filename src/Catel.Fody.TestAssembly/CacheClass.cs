// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CacheClass.cs" company="Catel development team">
//   Copyright (c) 2008 - 2013 Catel development team. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Catel.Fody.TestAssembly
{
    using System;
    using System.Threading;

    using Catel.Caching;

    public class CacheClass
    {
        #region Fields
        private readonly CacheStorage<string, string> ____GetFromCache2CacheStorage = new CacheStorage<string, string>();
        #endregion

        #region Methods
        [Cache("{0}")]
        public string GetFromCache(string theKey)
        {
            Thread.Sleep(5000);
            return theKey.ToUpper();
        } 
 
        public string GetFromCache2(string key)
        {
            string theKey = string.Format("{0}", key);
            Func<string> code = () => this.____GetFromCache2(key);
            return this.____GetFromCache2CacheStorage.GetFromCacheOrFetch(theKey, code, null, false);
        }

        private string ____GetFromCache2(string key)
        {
            Thread.Sleep(5000);
            return key.ToUpper();
        }

        #endregion
    }
}