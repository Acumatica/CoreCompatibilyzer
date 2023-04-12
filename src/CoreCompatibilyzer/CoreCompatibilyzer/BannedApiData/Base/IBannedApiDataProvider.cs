using System;
using System.Collections.Generic;

namespace CoreCompatibilyzer.BannedApiData
{
    public interface IBannedApiDataProvider
	{
        IEnumerable<BannedApi> GetBannedApiData();
    }
}
