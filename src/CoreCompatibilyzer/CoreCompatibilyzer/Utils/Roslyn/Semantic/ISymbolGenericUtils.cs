#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

using CoreCompatibilyzer.Utils.Common;

using Microsoft.CodeAnalysis;

namespace CoreCompatibilyzer.Utils.Roslyn.Semantic
{
	/// <summary>
	/// A generic utilities for <see cref="ISymbol"/>.
	/// </summary>
	public static class ISymbolGenericUtils
	{
		public static bool IsReadOnly(this ISymbol symbol) =>
			symbol.ThrowIfNull(nameof(symbol)) switch
			{
				IFieldSymbol field 		 => field.IsReadOnly,
				IPropertySymbol property => property.IsReadOnly,
				ITypeSymbol type 		 => type.IsReadOnly(),
				_ 						 => false
			};

		/// <summary>
		/// Check if <paramref name="symbol"/> is explicitly declared in the code.
		/// </summary>
		/// <param name="symbol">The symbol to check.</param>
		/// <returns>
		/// True if <paramref name="symbol"/> explicitly declared, false if not.
		/// </returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsExplicitlyDeclared(this ISymbol symbol) =>
			!symbol.ThrowIfNull(nameof(symbol)).IsImplicitlyDeclared && symbol.CanBeReferencedByName;

		public static bool IsDeclaredInType(this ISymbol symbol, ITypeSymbol? type)
		{
			symbol.ThrowIfNull(nameof(symbol));
		
			if (type == null || symbol.ContainingType == null)
				return false;

			return symbol.ContainingType.Equals(type, SymbolEqualityComparer.Default) || 
				   symbol.ContainingType.Equals(type.OriginalDefinition, SymbolEqualityComparer.Default);
		}

		/// <summary>
		/// Check if the <paramref name="symbol"/> has an attribute of a given <paramref name="attributeType"/>.
		/// </summary>
		/// <param name="symbol">The property to act on.</param>
		/// <param name="attributeType">Type of the attribute.</param>
		/// <param name="checkOverrides">True to check method overrides.</param>
		/// <param name="checkForDerivedAttributes">(Optional) True to check for attributes derived from <paramref name="attributeType"/>.</param>
		/// <returns>
		/// True if <paramref name="symbol"/> has attribute of <paramref name="attributeType"/>, false if not.
		/// </returns>
		public static bool HasAttribute<TSymbol>(this TSymbol symbol, INamedTypeSymbol attributeType, bool checkOverrides,
												 bool checkForDerivedAttributes = true)
		where TSymbol : class, ISymbol
		{
			symbol.ThrowIfNull(nameof(symbol));
			attributeType.ThrowIfNull(nameof(attributeType));

			Func<TSymbol, bool> attributeCheck = checkForDerivedAttributes
				? HasDerivedAttribute
				: HasAttribute;

			if (attributeCheck(symbol))
				return true;

			if (checkOverrides && symbol.IsOverride)
			{
				var overrides = symbol.GetOverridden();
				return overrides.Any(attributeCheck);
			}

			return false;

			//-----------------------------------------------------------
			bool HasAttribute(TSymbol symbolToCheck) =>
				symbolToCheck.GetAttributes()
							 .Any(a => attributeType.Equals(a.AttributeClass, SymbolEqualityComparer.Default));

			bool HasDerivedAttribute(TSymbol symbolToCheck) =>
				symbolToCheck.GetAttributes()
							 .Any(a => a.AttributeClass?.InheritsFromOrEquals(attributeType) ?? false);
		}

		/// <summary>
		/// Gets the <paramref name="symbol"/> and its overriden symbols.
		/// </summary>
		/// <param name="symbol">The symbol to act on.</param>
		/// <returns>
		/// The <paramref name="symbol"/> and its overriden symbols.
		/// </returns>
		public static IEnumerable<TSymbol> GetOverriddenAndThis<TSymbol>(this TSymbol symbol)
		where TSymbol : class, ISymbol
		{
			if (symbol.ThrowIfNull(nameof(symbol)).IsOverride)
				return GetOverriddenImpl(symbol, includeThis: true);
			else
				return new[] { symbol };
		}

		/// <summary>
		/// Gets the overriden symbols of <paramref name="symbol"/>.
		/// </summary>
		/// <param name="symbol">The symbol to act on.</param>
		/// <returns>
		/// The overriden symbols of <paramref name="symbol"/>.
		/// </returns>
		public static IEnumerable<TSymbol> GetOverridden<TSymbol>(this TSymbol symbol)
		where TSymbol : class, ISymbol
		{
			if (symbol.ThrowIfNull(nameof(symbol)).IsOverride)
				return GetOverriddenImpl(symbol, includeThis: false);
			else
				return Enumerable.Empty<TSymbol>();
		}

		private static IEnumerable<TSymbol> GetOverriddenImpl<TSymbol>(TSymbol symbol, bool includeThis)
		where TSymbol : class, ISymbol
		{
			TSymbol? current = includeThis ? symbol : symbol.OverriddenSymbol();

			while (current != null)
			{
				yield return current;
				current = current.OverriddenSymbol();
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static TSymbol? OverriddenSymbol<TSymbol>(this TSymbol symbol)
		where TSymbol : class, ISymbol
		{
			return symbol switch
			{
				IMethodSymbol method	 => method.OverriddenMethod as TSymbol,
				IPropertySymbol property => property.OverriddenProperty as TSymbol,
				IEventSymbol @event		 => @event.OverriddenEvent as TSymbol,
				_						 => null
			};
		}

		/// <summary>
		/// Gets Documentation ID for symbol.
		/// </summary>
		/// <remarks>
		/// <see cref="ISymbol.GetDocumentationCommentId"/> doesn't work correctly with methods because Roslyn removes braces for methods without parameters. This helper works with that problem.
		/// </remarks>
		/// <param name="symbol">The symbol to check.</param>
		/// <returns>
		/// The Documentation ID for symbol.
		/// </returns>
		public static string? GetDocID(this ISymbol symbol)
		{
			symbol.ThrowIfNull(nameof(symbol));
			string? docID = symbol.GetDocumentationCommentId().NullIfWhiteSpace();

			if (docID == null || symbol is not IMethodSymbol method || !method.Parameters.IsDefaultOrEmpty)
				return docID;

			return docID + "()";
		}
	}
}