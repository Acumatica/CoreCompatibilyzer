using System;
using System.Web;
using System.Web.Compilation;
using System.Web.Security;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DemoNetFramework
{
    public class Class1
    {
        public static void Abort() => Thread.CurrentThread.Abort("error");


        public static HttpContext GetHttpContext()
        {
            var type = System.Web.Compilation.PXBuildManager.GetType();

            if (type == null)
            {
                string error = "error";
				Thread.CurrentThread.Abort(error);
			}

			return HttpContext.Current;

		}
    }
}


namespace System.Web.Compilation
{
    public static class PXBuildManager
    {
        public static Type GetType() => throw new NotImplementedException();
    }
}