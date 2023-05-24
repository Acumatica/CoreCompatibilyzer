using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

using CoreCompatibilyzer.Utils.Common;

namespace CoreCompatibilyzer.ApiData.Errors
{
	/// <summary>
	/// A special exception class for reading api.
	/// </summary>
	[Serializable]
	public class ApiReaderException : Exception
	{
		private const string DefaultError = "An error happened during the reading of the API list.";

        public ApiReaderException() : base(DefaultError)
        {			
		}

        public ApiReaderException(string? message) : base(message.NullIfWhiteSpace() ?? DefaultError)
        {       
        }

        public ApiReaderException(string? message, Exception innerException) : base(message.NullIfWhiteSpace() ?? DefaultError, innerException)
        {
        }

        protected ApiReaderException(SerializationInfo info, StreamingContext context) : base(info, context) { }
	}
}
