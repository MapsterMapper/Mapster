using System;
using System.Linq.Expressions;

namespace Mapster
{
    public class PropertySetting
    {
        public bool Ignore { get; set; }
        public string? TargetPropertyName { get; set; }
        public Type? TargetPropertyType { get; set; }
        public LambdaExpression? MapFunc { get; set; }
    }
}