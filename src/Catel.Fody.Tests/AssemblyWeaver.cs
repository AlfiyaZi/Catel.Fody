// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AssemblyWeaver.cs" company="Catel development team">
//   Copyright (c) 2008 - 2013 Catel development team. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Catel.Fody;
using Catel.Fody.TestAssembly;
using Catel.Reflection;
using Mono.Cecil;

public static class AssemblyWeaver
{
    #region Constants
    public static Assembly Assembly;

    public static string BeforeAssemblyPath;
    public static string AfterAssemblyPath;

    public static List<string> Errors = new List<string>();
    #endregion

    #region Constructors
    static AssemblyWeaver()
    {
        //Force ref since MSTest is a POS
        var type = typeof (ViewModelBaseTest);

        BeforeAssemblyPath = type.GetAssemblyEx().Location;
        //BeforeAssemblyPath =  Path.GetFullPath("Catel.Fody.TestAssembly.dll");
        AfterAssemblyPath = BeforeAssemblyPath.Replace(".dll", "2.dll");

        Debug.WriteLine("Weaving assembly on-demand from '{0}' to '{1}'", BeforeAssemblyPath, AfterAssemblyPath);

        File.Copy(BeforeAssemblyPath, AfterAssemblyPath, true);

        var assemblyResolver = new MockAssemblyResolver();
        var moduleDefinition = ModuleDefinition.ReadModule(AfterAssemblyPath);

        var weavingTask = new ModuleWeaver
        {
            ModuleDefinition = moduleDefinition,
            AssemblyResolver = assemblyResolver,
            LogError = LogError,
        };

        weavingTask.Execute();
        moduleDefinition.Write(AfterAssemblyPath);

        if (Debugger.IsAttached)
        {
#if DEBUG
            var output = "debug";
#else
            var output = "release";
#endif

            var targetFile = $@"C:\Source\Catel.Fody\output\{output}\Catel.Fody.Tests\Catel.Fody.TestAssembly2.dll";
            var targetDirectory = Path.GetDirectoryName(targetFile);
            Directory.CreateDirectory(targetDirectory);
            File.Copy(AfterAssemblyPath, targetFile, true);
        }

        Assembly = Assembly.LoadFile(AfterAssemblyPath);
    }
#endregion

#region Methods
    private static void LogError(string error)
    {
        Errors.Add(error);
    }
#endregion
}