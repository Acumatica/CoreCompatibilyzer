using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

using CoreCompatibilyzer.Utils.Common;

namespace CoreCompatibilyzer.BannedApiData.Errors
{
	/// <summary>
	/// A special exception class for reading banned api.
	/// </summary>
	[Serializable]
	public class BannedApiReaderException : Exception
	{
		private const string DefaultError = "An error happened during the reading of the banned API list.";

        public BannedApiReaderException() : base(DefaultError)
        {			
		}

        public BannedApiReaderException(string? message) : base(message.NullIfWhiteSpace() ?? DefaultError)
        {       
        }

        public BannedApiReaderException(string? message, Exception innerException) : base(message.NullIfWhiteSpace() ?? DefaultError, innerException)
        {
        }

        protected BannedApiReaderException(SerializationInfo info, StreamingContext context) : base(info, context) { }
	}
}
