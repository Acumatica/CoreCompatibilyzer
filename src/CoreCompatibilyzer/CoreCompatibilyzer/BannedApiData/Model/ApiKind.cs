using System;
using System.Collections.Generic;
using System.Runtime.Versioning;

namespace CoreCompatibilyzer.BannedApiData.Model
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
