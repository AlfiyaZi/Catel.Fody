using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Catel.Fody;
using Catel.Fody.TestAssembly;
using Mono.Cecil;

public static class AssemblyWeaver
{

    public static Assembly Assembly;

    static AssemblyWeaver()
    {
		//Force ref since MSTest is a POS
	    var type = typeof (ViewModelBaseTest);
	    BeforeAssemblyPath = Path.GetFullPath("Catel.Fody.TestAssembly.dll");
		AfterAssemblyPath = BeforeAssemblyPath.Replace(".dll", "2.dll");

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

		Assembly = Assembly.LoadFile(AfterAssemblyPath);
    }

	public static string BeforeAssemblyPath;
	public static string AfterAssemblyPath;

	static void LogError(string error)
    {
        Errors.Add(error);
    }

    public static List<string> Errors = new List<string>();
}