using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace CoreCompatibilyzer.Utils.Common
{
	public static class StringExtensions
	{
		/// <summary>
		/// A string extension method that returns null if passed string <paramref name="str"/> is null, empty or contains only whitespaces.
		/// </summary>
		/// <param name="str">The string to act on.</param>
		/// <returns/>
		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string? NullIfWhiteSpace(this string? str) =>
			string.IsNullOrWhiteSpace(str)
				? null
				: str;

		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsNullOrWhiteSpace([NotNullWhen(returnValue: false)] this string? str) => string.IsNullOrWhiteSpace(str);

		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsNullOrEmpty([NotNullWhen(returnValue: false)] this string? str) => string.IsNullOrEmpty(str);

		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsEmpty(this string source) =>
			source.ThrowIfNull(nameof(source)).Length == 0;

		/// <summary>
		/// Joins strings in <paramref name="strings"/> with a <paramref name="separator"/>. 
		/// This extension method is just a shortcut for the call to <see cref="String.Join(string, IEnumerable{string})"/> which allows to use API in a fluent way. 
		/// </summary>
		/// <param name="strings">The strings to act on.</param>
		/// <param name="separator">The separator.</param>
		/// <returns>
		/// A joined string.
		/// </returns>
		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string Join(this IEnumerable<string> strings, string separator) => string.Join(separator, strings);
	}
}
