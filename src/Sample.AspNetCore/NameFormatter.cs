namespace Sample.AspNetCore
{
    public class NameFormatter
    {
        public string Format(string firstName, string lastName)
        {
            return $"{firstName} {lastName}";
        }
    }
}
