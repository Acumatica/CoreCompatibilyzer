using System;
using System.Collections.Generic;
using System.Linq;

using CoreCompatibilyzer.Constants;
using CoreCompatibilyzer.Utils.Common;

using Microsoft.CodeAnalysis;

namespace CoreCompatibilyzer.DotNetCompatibility
{
	public class DotNetVersionReader
	{
		private static readonly char[] _versionSeparators = new[] { ',' };
		private static readonly Dictionary<string, DotNetRuntime> _dotNetCoreVersions = new Dictionary<string, DotNetRuntime>
		{
			{ "2.1", DotNetRuntime.DotNetCore21 },
			{ "2.2", DotNetRuntime.DotNetCore22 },
			{ "3.0", DotNetRuntime.DotNetCore30 },
			{ "3.1", DotNetRuntime.DotNetCore31 },
			{ "5.0", DotNetRuntime.DotNet5 },
			{ "6.0", DotNetRuntime.DotNet6 },
		};

		private const string DotNetFrameworkPrefix = ".NETFramework";
		private const string DotNetCorePrefix = ".NETCoreApp";
		private const string VersionPrefix = "Version=v";

		public DotNetRuntime? TryParse(Compilation compilation)
		{
			compilation.ThrowIfNull(nameof(compilation));

			var targetFrameworkAttribute = compilation.GetTypeByMetadataName(TypeNames.TargetFrameworkAttribute);

			if (targetFrameworkAttribute == null)
				return null;

			var assemblyAttributes = compilation.Assembly.GetAttributes();

			if (assemblyAttributes.IsDefaultOrEmpty)
				return null;

			var targetFrameworkAttributeInfos = assemblyAttributes.Where(a => targetFrameworkAttribute.Equals(a.AttributeClass, SymbolEqualityComparer.Default));

			var parsedVersions = targetFrameworkAttributeInfos.Select(TryParseTargetFrameworkAttribute)
															  .Where(parsedVersion => parsedVersion != null)
															  .Select(parsedVersions => parsedVersions!.Value);
			DotNetRuntime? minSupportedVersion = null;

			foreach (DotNetRuntime version in parsedVersions)
			{
				if (minSupportedVersion == null || DotNetRunTimeComparer.Instance.Compare(minSupportedVersion.Value, version) > 0)
					minSupportedVersion = version;
				
			}

			return minSupportedVersion;
		}

		private DotNetRuntime? TryParseTargetFrameworkAttribute(AttributeData targetFrameworkAttributeInfo)
		{
			var constructorArgs = targetFrameworkAttributeInfo.ConstructorArguments;

			if (constructorArgs.IsDefaultOrEmpty)
				return null;

			var frameworkVersionConstructorArg = constructorArgs[0];

			if (frameworkVersionConstructorArg.Kind != TypedConstantKind.Primitive || frameworkVersionConstructorArg.Value is not string targetFrameworkVersion) 
				return null;

			return TryParseTargetFrameworkString(targetFrameworkVersion);
		}

		private DotNetRuntime? TryParseTargetFrameworkString(string rawTargetFrameworkVersion)
		{
			if (rawTargetFrameworkVersion.IsNullOrWhiteSpace())
				return null;

			var versionParts = rawTargetFrameworkVersion.Split(_versionSeparators, StringSplitOptions.RemoveEmptyEntries);

			if (versionParts.Length != 2)
				return null;

			var (runtimeType, runtimeRawVersion) = (versionParts[0], versionParts[1]);

			if (!runtimeRawVersion.StartsWith(VersionPrefix))
				return null;

			if (runtimeType == DotNetFrameworkPrefix)
				return DotNetRuntime.DotNetFramework;
			else if (runtimeType == DotNetCorePrefix)
			{
				string trimmedDotNetCoreVersion = runtimeRawVersion.Substring(VersionPrefix.Length);
				return _dotNetCoreVersions.TryGetValue(trimmedDotNetCoreVersion, out var coreRunTimeVersion)
					? coreRunTimeVersion
					: null;
			}

			return null;
		}
	}
}
