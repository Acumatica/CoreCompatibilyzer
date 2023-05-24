using System;
using System.Collections.Generic;
using System.Runtime.Versioning;

namespace CoreCompatibilyzer.ApiData.Model
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
