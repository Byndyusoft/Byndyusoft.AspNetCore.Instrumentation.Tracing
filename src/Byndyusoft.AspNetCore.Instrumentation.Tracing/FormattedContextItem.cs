namespace Byndyusoft.AspNetCore.Instrumentation.Tracing
{
    public struct FormattedContextItem
    {
        public string Name { get; }

        public object? FormattedValue { get; }

        public string Description { get; }

        public FormattedContextItem(string name, object? formattedValue, string description)
        {
            Name = name;
            FormattedValue = formattedValue;
            Description = description;
        }
    }
}