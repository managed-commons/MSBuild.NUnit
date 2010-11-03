/*
Copyright © 2005 Paul Welter. All rights reserved.
Copyright © 2010 MSBuild NUnit Task Project. All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions
are met:

1. Redistributions of source code must retain the above copyright
   notice, this list of conditions and the following disclaimer.
2. Redistributions in binary form must reproduce the above copyright
   notice, this list of conditions and the following disclaimer in the
   documentation and/or other materials provided with the distribution.
3. The name of the author may not be used to endorse or promote products
   derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE AUTHOR "AS IS" AND ANY EXPRESS OR
IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.
IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY DIRECT, INDIRECT,
INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF
THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE. 
*/

using System;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.Win32;

namespace MSBuild.Tasks
{

	public enum ProcessModelValues
	{
		Default,
		Single,
		Separate,
		Multiple
	}

	public enum AppDomainUsageValues
	{
		Default,
		None,
		Single,
		Multiple
	}

	/// <summary>
	/// Run NUnit on a group of assemblies.
	/// </summary>
	/// <example>Run NUnit tests.
	/// <code><![CDATA[
	/// <ItemGroup>
	///     <TestAssembly Include="C:\Program Files\NUnit 2.4\bin\*.tests.dll" />
	/// </ItemGroup>
	/// <Target Name="NUnit">
	///     <NUnit Assemblies="@(TestAssembly)" />
	/// </Target>
	/// ]]></code>
	/// </example>
	public class NUnit : ToolTask
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="T:NUnit"/> class.
		/// </summary>
		public NUnit ()
		{
			TestInNewThread = true;
		}

		/// <summary>
		/// Gets or sets the assemblies.
		/// </summary>
		/// <value>The assemblies.</value>
		[Required]
		public ITaskItem[] Assemblies { get; set; }

		/// <summary>
		/// Gets or sets the categories to include.
		/// </summary>
		/// <remarks>Multiple values must be separated by a comma ","</remarks>
		public string IncludeCategory { get; set; }

		/// <summary>
		/// Gets or sets the categories to exclude.
		/// </summary>
		/// <remarks>Multiple values must be separated by a comma ","</remarks>
		public string ExcludeCategory { get; set; }

		/// <summary>
		/// test case, fixture or namespace to run.
		/// </summary>
		/// <value>Name of the test case, fixture or namespace to run.</value>
		public string WhatToRun { get; set; }

		/// <summary>
		/// Gets or sets the framework version to use.
		/// </summary>
		/// <value>The framework version. Ex: net-2.0</value>
		public string FrameworkToUse { get; set; }

		/// <summary>
		/// Gets or sets the XSLT transform file.
		/// </summary>
		/// <value>The XSLT transform file.</value>
		public string XsltTransformFile { get; set; }

		/// <summary>
		/// Gets or sets the output XML file.
		/// </summary>
		/// <value>The output XML file.</value>
		public string OutputXmlFile { get; set; }

		/// <summary>
		/// The file to receive test error details.
		/// </summary>
		public string ErrorOutputFile { get; set; }

		/// <summary>
		/// Gets or sets the working directory.
		/// </summary>
		/// <value>The working directory.</value>
		/// <returns>
		/// The directory in which to run the executable file, or a null reference (Nothing in Visual Basic) if the executable file should be run in the current directory.
		/// </returns>
		public string WorkingDirectory { get; set; }

		/// <summary>
		/// Determines whether assemblies are copied to a shadow folder during testing.
		/// </summary>
		/// <remarks>Shadow copying is enabled by default. If you want to test the assemblies "in place",
		/// you must set this property to <c>True</c>.</remarks>
		public bool DisableShadowCopy { get; set; }

		/// <summary>
		/// The project configuration to run.
		/// </summary>
		/// <remarks>Only applies when a project file is used. The default is the first configuration, usually Debug.</remarks>
		public string ProjectConfiguration { get; set; }

		/// <summary>
		/// Allows tests to be run in a new thread, allowing you to take advantage of ApartmentState and ThreadPriority settings in the config file.
		/// </summary>
		public bool TestInNewThread { get; set; }


		/// <summary>
		/// Determines whether the tests are run in a 32bit process on a 64bit OS.
		/// </summary>
		public bool Force32Bit { get; set; }

		/// <summary>
		/// Determines whether to label each test in stdOut.
		/// </summary>
		public bool ShowLabels { get; set; }

		public ProcessModelValues processModel;

		public AppDomainUsageValues appDomainUsage;

		public string ProcessModel {
			get { return processModel.ToString (); }
			set { processModel = (ProcessModelValues)Enum.Parse (typeof(ProcessModelValues), value, true); }
		}
		public string AppDomainUsage {
			get { return appDomainUsage.ToString (); }
			set { appDomainUsage = (AppDomainUsageValues)Enum.Parse (typeof(AppDomainUsageValues), value, true); }
		}

		public bool HideDots { get; set; }

		public int TestCaseTimeoutInMilliseconds { get; set; }


		// Support /output=STR             File to receive test output (Short format: /out=STR)

		private readonly string Sep = IsLinuxOrMac ? "-" : "/";
		private static bool IsLinuxOrMac {
			get { return Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX; }
		}
		/// <summary>
		/// Returns a string value containing the command line arguments to pass directly to the executable file.
		/// </summary>
		/// <returns>
		/// A string value containing the command line arguments to pass directly to the executable file.
		/// </returns>
		protected override string GenerateCommandLineCommands ()
		{
			CommandLineBuilder builder = new CommandLineBuilder ();
			builder.AppendSwitch (Sep + "nologo");
			if (DisableShadowCopy) {
				builder.AppendSwitch (Sep + "noshadow");
			}
			if (HideDots) {
				builder.AppendSwitch (Sep + "nodots");
			}
			if (ShowLabels) {
				builder.AppendSwitch (Sep + "labels");
			}
			if (!TestInNewThread) {
				builder.AppendSwitch (Sep + "nothread");
			}
			builder.AppendFileNamesIfNotNull (Assemblies, " ");
			
			builder.AppendSwitchIfNotNull (Sep + "config=", ProjectConfiguration);
			
			builder.AppendSwitchIfNotNull (Sep + "run=", WhatToRun);
			
			builder.AppendSwitchIfNotNull (Sep + "include=", IncludeCategory);
			
			builder.AppendSwitchIfNotNull (Sep + "exclude=", ExcludeCategory);
			
			builder.AppendSwitchIfNotNull (Sep + "transform=", XsltTransformFile);
			
			builder.AppendSwitchIfNotNull (Sep + "xml=", OutputXmlFile);
			
			builder.AppendSwitchIfNotNull (Sep + "err=", ErrorOutputFile);
			
			builder.AppendSwitchIfNotNull (Sep + "framework=", FrameworkToUse);
			
			if (processModel != ProcessModelValues.Default)
				builder.AppendSwitchIfNotNull (Sep + "process=", processModel.ToString ());
			
			if (appDomainUsage != AppDomainUsageValues.Default)
				builder.AppendSwitchIfNotNull (Sep + "domain=", appDomainUsage.ToString ());
			
			if (TestCaseTimeoutInMilliseconds != 0)
				builder.AppendSwitchIfNotNull (Sep + "timeout=", TestCaseTimeoutInMilliseconds.ToString ());
			
			return builder.ToString ();
		}

		private void CheckToolPath ()
		{
			string nunitPath = ToolPath == null ? String.Empty : ToolPath.Trim ();
			if (String.IsNullOrEmpty (nunitPath)) {
				nunitPath = Path.GetDirectoryName (System.Reflection.Assembly.GetExecutingAssembly ().CodeBase);
				nunitPath = nunitPath.Replace (@"file:\", string.Empty);
			}
			
			if (!Directory.Exists (nunitPath))
				Log.LogError ("Could not find directory '{0}'", nunitPath);
			ToolPath = nunitPath;
		}

		/// <summary>
		/// Returns the fully qualified path to the executable file.
		/// </summary>
		/// <returns>
		/// The fully qualified path to the executable file.
		/// </returns>
		protected override string GenerateFullPathToTool ()
		{
			CheckToolPath ();
			return Path.Combine (ToolPath, ToolName);
		}

		/// <summary>
		/// Logs the starting point of the run to all registered loggers.
		/// </summary>
		/// <param name="message">A descriptive message to provide loggers, usually the command line and switches.</param>
		protected override void LogToolCommand (string message)
		{
			Log.LogCommandLine (MessageImportance.High, message);
		}

		/// <summary>
		/// Gets the name of the executable file to run.
		/// </summary>
		/// <value></value>
		/// <returns>The name of the executable file to run.</returns>
		protected override string ToolName {
			get {
				if (Force32Bit)
					return ToolFullName(@"nunit-console-x86");
				else
					return ToolFullName(@"nunit-console");
			}
		}

        private string ToolFullName(string toolname)
        {
            if (IsLinuxOrMac)
                return toolname;
            return toolname + ".exe";
        }

		/// <summary>
		/// Gets the <see cref="T:Microsoft.Build.Framework.MessageImportance"></see> with which to log errors.
		/// </summary>
		/// <value></value>
		/// <returns>The <see cref="T:Microsoft.Build.Framework.MessageImportance"></see> with which to log errors.</returns>
		protected override MessageImportance StandardOutputLoggingImportance {
			get { return MessageImportance.High; }
		}

		/// <summary>
		/// Returns the directory in which to run the executable file.
		/// </summary>
		/// <returns>
		/// The directory in which to run the executable file, or a null reference (Nothing in Visual Basic) if the executable file should be run in the current directory.
		/// </returns>
		protected override string GetWorkingDirectory ()
		{
			return string.IsNullOrEmpty (WorkingDirectory) ? base.GetWorkingDirectory () : WorkingDirectory;
		}
		
	}
}
