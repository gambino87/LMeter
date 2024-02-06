using System.Reflection;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System;

namespace LMeter.ACT
{
    public class TextTagFormatter
    {
        public static Regex TextTagRegex { get; } = new Regex(@"\[(\w+)(:k)?(?:\.(\d+))?(\|(\d+))?\]", RegexOptions.Compiled);

        private string _format;
        private Dictionary<string, MemberInfo> _members;
        private object _source;

        public TextTagFormatter(
            object source,
            string format,
            Dictionary<string, MemberInfo> members)
        {
            _source = source;
            _format = format;
            _members = members;
        }

        public string Evaluate(Match m)
        {
            if (m.Groups.Count < 5)
            {
                return m.Value;
            }
            
            string? value = null;
            string key = m.Groups[1].Value;
            string format = string.IsNullOrEmpty(m.Groups[3].Value) ? $"{_format}0" : $"{_format}{m.Groups[3].Value}";
            int? width = m.Groups[5].Success ? int.Parse(m.Groups[5].Value) : (int?)null; // Group 5 captures the width

            if (!_members.TryGetValue(key, out var fieldInfo))
            {
                return value ?? m.Value;
            }
            
            var memberValue = fieldInfo?.MemberType switch
            {
                MemberTypes.Field => ((FieldInfo)fieldInfo).GetValue(_source),
                MemberTypes.Property => ((PropertyInfo)fieldInfo).GetValue(_source),
                // Default should null because we don't want people accidentally trying to access a method and then throw an exception
                _ => null
            };

            if (memberValue is null)
            {
                return string.Empty;
            }

            if (memberValue is LazyFloat lazyFloat)
            {
                bool kilo = !string.IsNullOrEmpty(m.Groups[2].Value);
                value = lazyFloat.ToString(format, kilo) ?? m.Value;
            }
            else
            {
                value = memberValue.ToString();
                if (!string.IsNullOrEmpty(value) &&
                    int.TryParse(m.Groups[3].Value, out int trim) &&
                    trim < value.Length)
                {
                    value = memberValue?.ToString().AsSpan(0, trim).ToString();
                }
            }

            // Rest of your existing logic here to get the memberValue...

            // Apply the width specifier if present
            value = memberValue?.ToString() ?? "";
            if (width.HasValue)
            {
                value = value.PadLeft(width.Value);
            }

            return value ?? m.Value;
        }
    }
}