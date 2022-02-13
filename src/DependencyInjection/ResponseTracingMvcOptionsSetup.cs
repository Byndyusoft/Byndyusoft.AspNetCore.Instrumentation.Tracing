using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Byndyusoft.AspNetCore.Instrumentation.Tracing.DependencyInjection
{
    internal class ResponseTracingMvcOptionsSetup : IConfigureOptions<MvcOptions>
    {
        /// <inheritdoc />
        public void Configure(MvcOptions options)
        {
            options.Filters.Add<ResponseTracingFilter>();
        }
    }
}