using System;

namespace CoreCompatibilyzer.Runner
{
    /// <summary>
    /// Run result
    /// </summary>
    internal enum RunResult
    {
        /// <summary>
        /// The run finished successfully.
        /// </summary>
        Success = 0,

        /// <summary>
        /// The run was interrupted by a runtime error.
        /// </summary>
        RunTimeError = 1
    }

    /// <summary>
    /// The helper class for <see cref="RunResult"/>.
    /// </summary>
    internal static class RunResultHelper
    {
        public static int ToExitCode(this RunResult result) => (int)result;
    }
}
