using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Byndyusoft.AspNetCore.Instrumentation.Tracing
{
    public class BuildConfigurationTracingFilter : IAsyncActionFilter
    {
        private static readonly TextInfo TextInfo = new CultureInfo("en-US", false).TextInfo;
        private readonly Dictionary<string, string> _buildConfigurations;

        public BuildConfigurationTracingFilter()
        {
            _buildConfigurations = CollectionBuildConfigurations();
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var activity = Activity.Current;
            if (activity != null)
            {
                const string tagNamePrefix = "build.";

                foreach (var buildConfigurationValue in _buildConfigurations)
                    activity.SetTag($"{tagNamePrefix}{buildConfigurationValue.Key}", buildConfigurationValue.Value);
            }

            await next();
        }

        private static Dictionary<string, string> CollectionBuildConfigurations()
        {
            const string buildKeyPrefix = "BUILD_";

            var buildProperties = new Dictionary<string, string>();
            var variables = Environment.GetEnvironmentVariables();
            foreach (DictionaryEntry variable in variables)
            {
                if (variable.Value is null)
                    continue;

                var value = variable.Value.ToString();
                if (string.IsNullOrEmpty(value))
                    continue;

                var property = variable.Key.ToString();
                if (property is not null && property.StartsWith(buildKeyPrefix))
                {
                    var key = property.Remove(0, buildKeyPrefix.Length);
                    buildProperties.Add(EnvironmentKeyToCameCase(key), value);
                }
            }

            return buildProperties;
        }

        private static string EnvironmentKeyToCameCase(string environmentProperty)
        {
            var keyParts = environmentProperty.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => TextInfo.ToTitleCase(TextInfo.ToLower(x)));

            return string.Join("", keyParts);
        }
    }
}