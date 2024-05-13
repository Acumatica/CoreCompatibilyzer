using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace CoreCompatibilyzer.Utils.Common
{
	public static class ExceptionExtensions
	{
		private const string TheCollectionCannotBeEmptyErrorMsg = "The collection cannot be empty";

		/// <summary>
		/// An extension method for fluent patterns that throws <see cref="ArgumentNullException"/> if <paramref name="obj"/> is null.
		/// Otherwise returns the <paramref name="obj"/>.
		/// </summary>
		/// <typeparam name="T">Object type.</typeparam>
		/// <param name="obj">The object to act on.</param>
		/// <param name="paramName">(Optional) Name of the parameter for <see cref="ArgumentNullException"/>.</param>
		/// <param name="message">(Optional) The error message.</param>
		/// <returns/>
		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[return: NotNullIfNotNull("obj")]
		public static T ThrowIfNull<T>(this T? obj, string? paramName = null, string? message = null) =>
			obj ?? throw NewArgumentNullException(paramName, message);
		
		/// <summary>
		/// An extension method for fluent patterns that throws <see cref="ArgumentNullException"/> if <paramref name="collection"/> is null,
		/// throws <see cref="ArgumentException"/> if collection is empty.
		/// Otherwise returns the <paramref name="collection"/>.
		/// </summary>
		/// <typeparam name="T">Object type.</typeparam>
		/// <param name="collection">The collection to act on.</param>
		/// <param name="paramName">(Optional) Name of the parameter for <see cref="ArgumentNullException"/> or <see cref="ArgumentException"/>.</param>
		/// <param name="message">(Optional) The error message.</param>
		/// <returns/>
		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[return: NotNullIfNotNull("collection")]
		public static IEnumerable<T> ThrowIfNullOrEmpty<T>(this IEnumerable<T>? collection, string? paramName = null, string? message = null)
		{
			if (collection == null)
			{
				throw NewArgumentNullException(paramName, message);
			}
			else if (collection.IsEmpty())
			{
				message = message.IsNullOrWhiteSpace()
							? TheCollectionCannotBeEmptyErrorMsg
							: message;
				throw NewArgumentException(paramName, message);
			}

			return collection;
		}

		/// <summary>
		/// An extension method for fluent patterns that throws <see cref="ArgumentNullException"/> if <paramref name="str"/> is null and
		/// throws <see cref="ArgumentException"/> if <paramref name="str"/> contains only whitespaces or is empty.
		/// Otherwise returns the <paramref name="str"/>.
		/// </summary>
		/// <param name="str">The string to act on.</param>
		/// <param name="paramName">(Optional) Name of the parameter for <see cref="ArgumentNullException"/> or <see cref="ArgumentException"/>.</param>
		/// <param name="message">(Optional) The error message.</param>
		/// <returns/>
		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[return: NotNullIfNotNull("str")]
		public static string ThrowIfNullOrWhiteSpace(this string? str, string? paramName = null, string? message = null)
		{
			if (!string.IsNullOrWhiteSpace(str))
				return str!;

			throw str == null
				? NewArgumentNullException(paramName, message)
				: NewArgumentException(paramName, message);
		}

		private static ArgumentNullException NewArgumentNullException(string? parameter = null, string? message = null)
		{
			return parameter == null
			   ? new ArgumentNullException()
			   : message == null
				   ? new ArgumentNullException(parameter)
				   : new ArgumentNullException(parameter, message);
		}

		private static ArgumentException NewArgumentException(string? parameter = null, string? message = null)
		{
			return parameter == null
			   ? new ArgumentException()
			   : message == null
				   ? new ArgumentException(parameter)
				   : new ArgumentException(parameter, message);
		}
	}
}
