using Mapster.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mapster
{
    [AdaptWith(AdaptDirectives.DestinationAsRecord)]
    public class OverrideTypesSettings : TypeAdapterSettings
    {
        public List<string> SkipSettings
        {
            get => Get(nameof(SkipSettings), () => new List<string>());
        }

        public IEnumerable<string> ReMapDestination
        {
            get => this.Resolvers.Select(x=>x.DestinationMemberName).Union(ReMapDestinationMembers);
        }

        public bool? ReMapExtraSource
        {
            get => Get(nameof(ReMapExtraSource));
            set => Set(nameof(ReMapExtraSource), value);
        }

        public bool? SkipAllSettings
        {
            get => Get(nameof(SkipAllSettings));
            set => Set(nameof(SkipAllSettings), value);
        }

        public override void Apply(object other)
        {
            if (other is SettingStore settingStore)
                Apply(settingStore);
        }

        public override void Apply(SettingStore other)
        {
            if(!SkipAllSettings.GetValueOrDefault())
                base.ApplyWithSkipSettings(other, SkipSettings);
        }

        public List<InvokerModel> ApplyResolversOnly(TypeAdapterSettings other)
        {
            var result = new List<InvokerModel>(this.Resolvers);
            var seen = new HashSet<InvokerModel>(result,new InvokerModelApplyComparer());

            foreach (var item in other.Resolvers)
            {
                if (seen.Add(item))
                {
                    result.Add(item);
                }
            }
            return result;
        }

        public OverrideTypesSettings CloneOnlySkipSettings()
        {
            var result = new OverrideTypesSettings();

            result.SkipAllSettings = this.SkipAllSettings;
            result.SkipSettings.AddRange(this.SkipSettings);
            result.ReMapExtraSource = this.ReMapExtraSource;

            return result;
        }
    }
}
