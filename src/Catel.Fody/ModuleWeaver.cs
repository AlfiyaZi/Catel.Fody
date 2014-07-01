﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ModuleWeaver.cs" company="Catel development team">
//   Copyright (c) 2008 - 2013 Catel development team. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace Catel.Fody
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Xml.Linq;

    using Services;

    using Mono.Cecil;
    using Mono.Cecil.Cil;

    public class ModuleWeaver
    {
        public ModuleWeaver()
        {
            // Init logging delegates to make testing easier
            LogInfo = s => { };
            LogWarning = s => { };
            LogError = s => { };
        }

        public XElement Config { get; set; }

        public Action<string> LogInfo { get; set; }
        public Action<string> LogWarning { get; set; }
        public Action<string, SequencePoint> LogWarningPoint { get; set; }
        public Action<string> LogError { get; set; }
        public Action<string, SequencePoint> LogErrorPoint { get; set; }

        public IAssemblyResolver AssemblyResolver { get; set; }
        public ModuleDefinition ModuleDefinition { get; set; }

        public void Execute()
        {
            try
            {
                //#if DEBUG
                //                Debugger.Launch();
                //#endif

                // Clear cache because static members will be re-used over multiple builds over multiple systems
                CacheHelper.ClearAllCaches();

                var configuration = new Configuration(Config);

                InitializeEnvironment();

                // 1st step: set up the basics
                var msCoreReferenceFinder = new MsCoreReferenceFinder(this, ModuleDefinition.AssemblyResolver);
                msCoreReferenceFinder.Execute();

                // Note: nested types not supported because we only list actual types (thus not nested)
                var types = ModuleDefinition.GetTypes().Where(x => x.IsClass && x.BaseType != null).ToList();

                var typeNodeBuilder = new CatelTypeNodeBuilder(types);
                typeNodeBuilder.Execute();

                // Remove any code generated types from the list of types to process
                var codeGenTypeCleaner = new CodeGenTypeCleaner(typeNodeBuilder);
                codeGenTypeCleaner.Execute();

                // 2nd step: Auto property weaving
                if (configuration.WeaveProperties)
                {
                    FodyEnvironment.LogInfo("Weaving properties");

                    var propertyWeaverService = new AutoPropertiesWeaverService(typeNodeBuilder, msCoreReferenceFinder);
                    propertyWeaverService.Execute();
                }
                else
                {
                    FodyEnvironment.LogInfo("Weaving properties is disabled");
                }

                // 3rd step: Exposed properties weaving
                if (configuration.WeaveExposedProperties)
                {
                    FodyEnvironment.LogInfo("Weaving exposed properties");

                    var exposedPropertiesWeaverService = new ExposedPropertiesWeaverService(typeNodeBuilder, msCoreReferenceFinder);
                    exposedPropertiesWeaverService.Execute();
                }
                else
                {
                    FodyEnvironment.LogInfo("Weaving exposed properties is disabled");
                }

                // 4th step: Argument weaving
                if (configuration.WeaveArguments)
                {
                    FodyEnvironment.LogInfo("Weaving arguments");

                    var argumentWeaverService = new ArgumentWeaverService(types);
                    argumentWeaverService.Execute();
                }
                else
                {
                    FodyEnvironment.LogInfo("Weaving arguments is disabled");
                }

                // 5th step: Logging weaving
                if (configuration.WeaveLogging)
                {
                    FodyEnvironment.LogInfo("Weaving logging");

                    var loggingWeaver = new LoggingWeaverService(types);
                    loggingWeaver.Execute();
                }
                else
                {
                    FodyEnvironment.LogInfo("Weaving logging is disabled");
                }

                // 6th step: Xml schema weaving
                if (configuration.GenerateXmlSchemas)
                {
                    FodyEnvironment.LogInfo("Weaving xml schemas");

                    var xmlSchemasWeaverService = new XmlSchemasWeaverService(msCoreReferenceFinder, typeNodeBuilder);
                    xmlSchemasWeaverService.Execute();
                }
                else
                {
                    FodyEnvironment.LogInfo("Weaving xml schemas is disabled");
                }

                // Last step: clean up
                var referenceCleaner = new ReferenceCleaner(this);
                referenceCleaner.Execute();
            }
            catch (Exception ex)
            {
                LogError(ex.Message);

#if DEBUG
                Debugger.Launch();
#endif
            }
        }

        private void InitializeEnvironment()
        {
            FodyEnvironment.ModuleDefinition = ModuleDefinition;
            FodyEnvironment.AssemblyResolver = AssemblyResolver;

            FodyEnvironment.Config = Config;
            FodyEnvironment.LogInfo = LogInfo;
            FodyEnvironment.LogWarning = LogWarning;
            FodyEnvironment.LogWarningPoint = LogWarningPoint;
            FodyEnvironment.LogError = LogError;
            FodyEnvironment.LogErrorPoint = LogErrorPoint;

            var assemblyResolver = ModuleDefinition.AssemblyResolver;

            try
            {
                FodyEnvironment.IsCatelCoreAvailable = assemblyResolver.Resolve("Catel.Core") != null;
            }
            catch (Exception)
            {
                LogError("Catel.Core is not references, cannot weave without a Catel.Core reference");
            }

            try
            {
                FodyEnvironment.IsCatelMvvmAvailable = assemblyResolver.Resolve("Catel.MVVM") != null;
            }
            catch (Exception)
            {
                LogInfo("Catel.MVVM is not referenced, skipping Catel.MVVM specific functionality");
            }
        }
    }
}