using System;
using System.Web;
using System.Web.Compilation;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DemoNetFramework
{
    public class Class1
    {
        public static void Abort() => Thread.CurrentThread.Abort();


        public static HttpContext GetHttpContext()
        {
            return HttpContext.Current;

		}
    }
}
