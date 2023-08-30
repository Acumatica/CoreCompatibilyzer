using System;
using System.Linq;
using System.Collections.Generic;

using CoreCompatibilyzer.ApiData.Model;
using CoreCompatibilyzer.Runner.Input;
using CoreCompatibilyzer.Utils.Common;
using CoreCompatibilyzer.Runner.Output.Data;

using Microsoft.CodeAnalysis;

namespace CoreCompatibilyzer.Runner.Output
{
	internal class UsedDistinctApisCalculator
	{
		private readonly HashSet<string> _usedNamespaces;
		private readonly HashSet<string> _usedBannedTypes;

		private readonly bool _effectiveShowMemberOfUsedType;

		public UsedDistinctApisCalculator(AppAnalysisContext analysisContext, HashSet<string> usedNamespaces, HashSet<string> usedBannedTypes)
		{
			analysisContext.ThrowIfNull(nameof(analysisContext));

			_usedNamespaces  = usedNamespaces ?? new HashSet<string>();
			_usedBannedTypes = usedBannedTypes ?? new HashSet<string>();

			_effectiveShowMemberOfUsedType = analysisContext.ReportMode == ReportMode.UsedAPIsWithUsages || // Always display members of used types if the report is configured to display usages
											 analysisContext.ShowMembersOfUsedType;
		}

		/// <summary>
		/// Gets all unsorted used apis in the <paramref name="unsortedApis"/>.
		/// </summary>
		/// <param name="unsortedApis">The unsorted APIs.</param>
		/// <returns/>
		public IEnumerable<Api> GetAllUsedApis(IEnumerable<Api> unsortedApis) =>
			GetAllUsedApisImpl(unsortedApis.ThrowIfNull(nameof(unsortedApis)));

		/// <summary>
		/// Gets all unsorted used apis in the <paramref name="unsortedDiagnostics"/>.
		/// </summary>
		/// <param name="unsortedDiagnostics">The unsorted diagnostics.</param>
		/// <returns/>
		public IEnumerable<Api> GetAllUsedApis(IEnumerable<(Diagnostic Diagnostic, Api BannedApi)> unsortedDiagnostics)
		{
			var unsortedApis = unsortedDiagnostics.ThrowIfNull(nameof(unsortedDiagnostics)).Select(d => d.BannedApi);
			return GetAllUsedApisImpl(unsortedApis);
		}

		private IEnumerable<Api> GetAllUsedApisImpl(IEnumerable<Api> unsortedApis)
		{
			var distinctApis = unsortedApis.Distinct();

			foreach (Api api in distinctApis)
			{
				switch (api.Kind)
				{
					case ApiKind.Namespace:
						if (_usedNamespaces.Contains(api.Namespace))
							yield return api;

						continue;

					case ApiKind.Type
					when _effectiveShowMemberOfUsedType || api.AllContainingTypes.IsDefaultOrEmpty || !AreContainingTypesUsed(api):
						yield return api;
						continue;

					case ApiKind.Field:
					case ApiKind.Property:
					case ApiKind.Event:
					case ApiKind.Method:
						if (_effectiveShowMemberOfUsedType || !_usedBannedTypes.Contains(api.FullTypeName))
							yield return api;

						continue;
				}
			}
		}

		private bool AreContainingTypesUsed(Api api)
		{
			string containingTypeName = $"{api.Namespace}";

			for (int i = 0; i < api.AllContainingTypes.Length; i++)
			{
				containingTypeName += $".{api.AllContainingTypes[i]}";

				if (_usedBannedTypes!.Contains(containingTypeName))
					return true;
			}

			return false;
		}
	}
}
