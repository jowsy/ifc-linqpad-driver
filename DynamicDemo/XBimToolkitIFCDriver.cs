using LINQPad.Extensibility.DataContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xbim.Common;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.SharedBldgElements;

namespace DataContextDriverDemo.DynamicDemo
{
    public class XBimToolkitIFCDriver : DynamicDataContextDriver
    {
        static XBimToolkitIFCDriver()
        {
            // Uncomment the following code to attach to Visual Studio's debugger when an exception is thrown.
            //AppDomain.CurrentDomain.FirstChanceException += (sender, args) =>
            //{
            //	if (args.Exception.StackTrace.Contains (typeof (DynamicDemoDriver).Namespace))
            //		Debugger.Launch ();
            //};
        }

        public override string Name => "XBim Toolkit IFC Driver";

        public override string Author => "Joel Waldheim Saury";

        public override void InitializeContext(IConnectionInfo cxInfo, object context, QueryExecutionManager executionManager)
        {
            base.InitializeContext(cxInfo, context, executionManager);
        }

        public override string GetConnectionDescription(IConnectionInfo cxInfo)
            => "IFC file path: " + new IfcConnectionOptions(cxInfo).IfcFilePath;

        public override bool ShowConnectionDialog(IConnectionInfo cxInfo, ConnectionDialogOptions dialogOptions)
            => new IfcConnectionDialog(cxInfo).ShowDialog() == true;

        public override List<ExplorerItem> GetSchemaAndBuildAssembly(
            IConnectionInfo cxInfo, AssemblyName assemblyToBuild, ref string nameSpace, ref string typeName)
        {
            string ifcFilePath = new IfcConnectionOptions(cxInfo).IfcFilePath;

            string source = @"using System;
using System.Collections.Generic;
using System.Linq;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

namespace " + nameSpace + @"
{
    // The main typed data class. The user's queries subclass this, so they have easy access to all its members.
	public class " + typeName + @"
	{
       public " + typeName + @"(IfcStore  model)
        {
            this.Model = model;
        }

        public IfcStore Model
        {
            get;
            private set;
        }

  	    public IEnumerable<IIfcWall> IfcWalls => Model.Instances.OfType<IIfcWall>();
		public IEnumerable<IIfcSpace> IfcSpaces => Model.Instances.OfType<IIfcSpace>();
	}
}";
            Compile(source, assemblyToBuild.CodeBase, cxInfo);

            var walls =
                new ExplorerItem("IfcWalls", ExplorerItemKind.QueryableObject, ExplorerIcon.Table)
                {
                    IsEnumerable = true,
                    DragText = "IfcWalls",
                    Children = new List<ExplorerItem>()
                    {
                          new ExplorerItem("Name", ExplorerItemKind.Property, ExplorerIcon.Column)
                    }
                };
            var spaces =
            new ExplorerItem("IfcSpaces", ExplorerItemKind.QueryableObject, ExplorerIcon.Table)
            {
                IsEnumerable = true,
                DragText = "IfcSpaces",
                Children = new List<ExplorerItem>()
                {
                          new ExplorerItem("Name", ExplorerItemKind.Property, ExplorerIcon.Column)
                }
            };

            return new[] { walls, spaces }.ToList();
        }

        public override IEnumerable<string> GetAssembliesToAdd(IConnectionInfo cxInfo)
        {
            return new string[]
                {
                    "Xbim.Common.dll",
                    "Xbim.Ifc.dll",
                    "Xbim.Ifc2x3.dll",
                    "Xbim.Ifc4.dll",
                    "Xbim.IO.MemoryModel.dll",
                    "Xbim.Tessellator.dll",
                };
        }

        public override IEnumerable<string> GetNamespacesToAdd(IConnectionInfo cxInfo)
        {
            return new string[]
                {
                    "Xbim.Ifc",
                    "Xbim.Ifc4.Interfaces"
                };
        }

        public override object[] GetContextConstructorArguments(IConnectionInfo cxInfo)
        {
            string ifcFilePath = new IfcConnectionOptions(cxInfo).IfcFilePath;

            return new object[]
                {
                        IfcStore.Open(ifcFilePath)
                };
        }

        public override ParameterDescriptor[] GetContextConstructorParameters(IConnectionInfo cxInfo)
        {
            return new[]
            {
                new ParameterDescriptor("model", typeof(IfcStore).FullName)
            };
        }

        private static void Compile(string cSharpSourceCode, string outputFile, IConnectionInfo cxInfo)
        {
            var customAssemblies = new[]{
                typeof(IfcStore).Assembly.Location,
                typeof(IIfcWall).Assembly.Location,
                typeof(IfcWall).Assembly.Location,
                typeof(IEntityCollection).Assembly.Location
            };

            string[] assembliesToReference =
#if NETCORE
            // GetCoreFxReferenceAssemblies is helper method that returns the full set of .NET Core reference assemblies.
            // (There are more than 100 of them.)
            GetCoreFxReferenceAssemblies(cxInfo).Concat(customAssemblies).ToArray();
#else
			// .NET Framework - here's how to get the basic Framework assemblies:
			new[]
			{
				typeof (int).Assembly.Location,            // mscorlib
				typeof (Uri).Assembly.Location,            // System
				typeof (XmlConvert).Assembly.Location,     // System.Xml
				typeof (Enumerable).Assembly.Location,     // System.Core
				typeof (DataSet).Assembly.Location         // System.Data
			};
#endif
            // CompileSource is a static helper method to compile C# source code using LINQPad's built-in Roslyn libraries.
            // If you prefer, you can add a NuGet reference to the Roslyn libraries and use them directly.

            var compileResult = CompileSource(new CompilationInput
            {
                FilePathsToReference = assembliesToReference,
                OutputPath = outputFile,
                SourceCode = new[] { cSharpSourceCode }
            });

            if (compileResult.Errors.Length > 0)
                throw new Exception("Cannot compile typed context: " + compileResult.Errors[0]);
        }
    }
}
