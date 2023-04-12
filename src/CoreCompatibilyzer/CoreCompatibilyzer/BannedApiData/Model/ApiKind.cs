using System;
using System.Collections.Generic;
using System.Runtime.Versioning;

namespace CoreCompatibilyzer.BannedApiData
{
	public enum ApiKind
	{
		Undefined,
		Namespace,
		Type,
		Field,
		Property,
		Event,
		Method
	}
}
