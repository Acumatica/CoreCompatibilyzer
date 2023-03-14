using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;

using CoreCompatibilyzer.Utils.Common;

using Microsoft.CodeAnalysis;

namespace CoreCompatibilyzer.DotNetCompatibility
{
	public sealed class DotNetVersionsStorage
	{
		private readonly DotNetVersionReader _dotNetVersionReader = new();

		// use 1 lock per assembly
		private readonly ConcurrentDictionary<AssemblyIdentity, object> _assemblyLocks = new();

		// Use simple dictionary for storing runtime versions 
		private readonly Dictionary<AssemblyIdentity, DotNetRuntime?> _assembliesRunTimeVersions = new();

		public static DotNetVersionsStorage Instance 
		{ 
			get;
		} = new DotNetVersionsStorage();

		private DotNetVersionsStorage()
		{
		}

		public DotNetRuntime? GetDotNetRuntimeVersion(Compilation compilation)
		{
			compilation.ThrowIfNull(nameof(compilation));

			AssemblyIdentity identity = compilation.Assembly.Identity;

			if (_assembliesRunTimeVersions.TryGetValue(identity, out DotNetRuntime? runtimeVersion))
				return runtimeVersion;

			var assemblyLocker = _assemblyLocks.GetOrAdd(identity, key => new object());

			lock (assemblyLocker) 
			{
				if (_assembliesRunTimeVersions.TryGetValue(identity, out runtimeVersion))
					return runtimeVersion;

				runtimeVersion = _dotNetVersionReader.GetRuntimeVersion(compilation);
				_assembliesRunTimeVersions[identity] = runtimeVersion;

				return runtimeVersion;
			}
		}
	}
}
