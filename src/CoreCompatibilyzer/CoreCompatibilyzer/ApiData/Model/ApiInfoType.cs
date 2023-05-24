using System;
using System.Collections.Generic;

namespace CoreCompatibilyzer.BannedApiData.Model
{
	public enum ApiInfoType : byte
	{
		NotPresentInNetCore,
		Obsolete,
		WhiteList
	}
}
