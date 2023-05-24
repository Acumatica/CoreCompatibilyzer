using System;
using System.Collections.Generic;

namespace CoreCompatibilyzer.ApiData.Model
{
	public enum ApiInfoType : byte
	{
		NotPresentInNetCore,
		Obsolete,
		WhiteList
	}
}
