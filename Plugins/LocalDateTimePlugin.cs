using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace SKLocalRAGSearchWithFunctionCalling.Plugins
{
    public sealed class LocalDateTimePlugin
    {
        [KernelFunction, Description("Retrieves the current date and time in Local Time.")]
        public static String GetCurrentLocalDateTime()
        {
            return DateTime.Now.ToLocalTime().ToString();
        }
    }
}
