// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ArgumentFacts.cs" company="Catel development team">
//   Copyright (c) 2008 - 2013 Catel development team. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Catel.Fody.Test
{
    using System;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class CacheFacts
    {
        [TestMethod]
        public void SecondCallIsFasterThanFirstOne()
        {
            var type = AssemblyWeaver.Assembly.GetType("Catel.Fody.TestAssembly.CacheClass");

            var instance = Activator.CreateInstance(type);

            var method = type.GetMethod("GetFromCache");

            DateTime startTime = DateTime.Now;
            method.Invoke(instance, new object[] { "theKey" });
            TimeSpan firstCallElapsedTime = DateTime.Now.Subtract(startTime);     
            
            startTime = DateTime.Now;
            method.Invoke(instance, new object[] { "theKey" });
            TimeSpan secondCallElapsedTime = DateTime.Now.Subtract(startTime);

            Assert.IsTrue(secondCallElapsedTime < firstCallElapsedTime);
        }
    }
}