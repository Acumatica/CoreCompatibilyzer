using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

using CoreCompatibilyzer.BannedApiData.Model;
using CoreCompatibilyzer.StaticAnalysis.ApiInfoRetrievers;
using CoreCompatibilyzer.Utils.Roslyn.Semantic;

using Microsoft.CodeAnalysis;

namespace CoreCompatibilyzer.StaticAnalysis
{
	public partial class CoreCompatibilyzerAnalyzer
	{
		private class BannedTypesInfoCollector
		{
			private readonly CancellationToken _cancellation;
			private readonly IApiInfoRetriever _apiBanInfoRetriever;
			private readonly HashSet<ITypeSymbol> _checkedTypes = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);

            public BannedTypesInfoCollector(IApiInfoRetriever apiBanInfoRetriever, CancellationToken cancellation)
            {
				_apiBanInfoRetriever = apiBanInfoRetriever;
				_cancellation = cancellation;
            }

			public List<Api>? GetTypeParameterBannedApiInfos(ITypeParameterSymbol typeParameterSymbol, bool checkInterfaces)
			{
				_checkedTypes.Clear();
				return GetBannedInfosFromTypeParameter(typeParameterSymbol, alreadyCollectedInfos: null, checkInterfaces);
			}

			public List<Api>? GetTypeBannedApiInfos(ITypeSymbol typeSymbol, bool checkInterfaces)
			{				
				_checkedTypes.Clear();
				return GetBannedInfosFromTypeSymbolAndItsHierarchy(typeSymbol, alreadyCollectedInfos: null, checkInterfaces);
			}

			private List<Api>? GetBannedInfosFromTypeSymbolAndItsHierarchy(ITypeSymbol typeSymbol, List<Api>? alreadyCollectedInfos, 
																				 bool checkInterfaces)
			{
				if (!_checkedTypes.Add(typeSymbol))
					return alreadyCollectedInfos;

				_cancellation.ThrowIfCancellationRequested();

				if (typeSymbol.SpecialType != SpecialType.None)
					return alreadyCollectedInfos;

				alreadyCollectedInfos = GetBannedInfosFromType(typeSymbol, alreadyCollectedInfos, checkInterfaces);
				alreadyCollectedInfos = GetBannedInfosFromBaseTypes(typeSymbol, alreadyCollectedInfos, checkInterfaces);

				_cancellation.ThrowIfCancellationRequested();

				if (!checkInterfaces)
					return alreadyCollectedInfos;

				var interfaces = typeSymbol.AllInterfaces;

				if (interfaces.IsDefaultOrEmpty)
					return alreadyCollectedInfos;

				foreach (INamedTypeSymbol @interface in interfaces)
				{
					if (_checkedTypes.Add(@interface))
					{
						_cancellation.ThrowIfCancellationRequested();
						alreadyCollectedInfos = GetBannedInfosFromType(@interface, alreadyCollectedInfos, checkInterfaces);
					}
				}

				_cancellation.ThrowIfCancellationRequested();
				return alreadyCollectedInfos;
			}

			private List<Api>? GetBannedInfosFromBaseTypes(ITypeSymbol typeSymbol, List<Api>? alreadyCollectedInfos, bool checkInterfaces)
			{
				if (typeSymbol.IsStatic || typeSymbol.TypeKind != TypeKind.Class)
					return alreadyCollectedInfos;

				foreach (var baseType in typeSymbol.GetBaseTypes())
				{
					_cancellation.ThrowIfCancellationRequested();

					if (!_checkedTypes.Add(baseType) || baseType.SpecialType != SpecialType.None)
						return alreadyCollectedInfos;

					int oldCount = alreadyCollectedInfos?.Count ?? 0;
					alreadyCollectedInfos = GetBannedInfosFromType(typeSymbol, alreadyCollectedInfos, checkInterfaces);

					// If we found something incompatible there is no need to go lower. We don't need to report whole incompatible inheritance chain
					int newCount = alreadyCollectedInfos?.Count ?? 0;

					if (oldCount != newCount)
						return alreadyCollectedInfos;
				}

				return alreadyCollectedInfos;
			}

			private List<Api>? GetBannedInfosFromType(ITypeSymbol typeSymbol, List<Api>? alreadyCollectedInfos, 
															bool checkInterfaces)
			{
				if (_apiBanInfoRetriever.GetInfoForApi(typeSymbol) is Api bannedTypeInfo)
				{
					alreadyCollectedInfos ??= new List<Api>(capacity: 4);
					alreadyCollectedInfos.Add(bannedTypeInfo);
				}

				_cancellation.ThrowIfCancellationRequested();

				if (typeSymbol is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.IsGenericType)
					alreadyCollectedInfos = GetBannedApisFromTypesList(namedTypeSymbol.TypeArguments, alreadyCollectedInfos, checkInterfaces);

				return alreadyCollectedInfos;
			}

			private List<Api>? GetBannedInfosFromTypeParameter(ITypeParameterSymbol typeParameterSymbol, List<Api>? alreadyCollectedInfos, 
																	 bool checkInterfaces)
			{
				if (!_checkedTypes.Add(typeParameterSymbol))
					return alreadyCollectedInfos;

				return GetBannedApisFromTypesList(typeParameterSymbol.ConstraintTypes, alreadyCollectedInfos, checkInterfaces);
			}

			private List<Api>? GetBannedApisFromTypesList(ImmutableArray<ITypeSymbol> types, List<Api>? alreadyCollectedInfos, 
																bool checkInterfaces)
			{
				if (types.IsDefaultOrEmpty)
					return alreadyCollectedInfos;

				foreach (ITypeSymbol constraintType in types)
				{
					_cancellation.ThrowIfCancellationRequested();

					switch (constraintType)
					{
						case ITypeParameterSymbol otherTypeParameter:
							alreadyCollectedInfos = GetBannedInfosFromTypeParameter(otherTypeParameter, alreadyCollectedInfos, checkInterfaces);
							continue;

						case INamedTypeSymbol namedType:
							alreadyCollectedInfos = GetBannedInfosFromTypeSymbolAndItsHierarchy(namedType, alreadyCollectedInfos, checkInterfaces);
							continue;
					}
				}

				return alreadyCollectedInfos;
			}
		}
	}
}
