using System;

namespace Mapster
{
    public class TypeAdapterRule
    {
        public Func<PreCompileArgument, int?> Priority { get; set; }
        public TypeAdapterSettings Settings { get; set; }

        public void LoadLasyInherits(TypeAdapterRule rule)
        {
            this.Settings.Apply(rule.Settings);
        }
    }
}