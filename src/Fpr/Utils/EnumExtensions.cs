namespace Fpr.Utils
{
    public static class EnumExtensions
    {
        public static string ToFastString<TEnum>(this TEnum val)
           where TEnum : struct
        {
            return FastEnum<TEnum>.ToString(val);
        }

        public static TEnum ToFastEnum<TEnum>(this string val) where TEnum : struct
        {
            return FastEnum<TEnum>.ToEnum(val);
        }
    }
}