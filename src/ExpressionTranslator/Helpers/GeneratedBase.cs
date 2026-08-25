namespace ExpressionDebugger.Helpers
{
    public abstract class GeneratedBase 
    {
        public override bool Equals(object obj)
        {
            if(obj is null)
                return base.Equals(obj);
            else
                return this.GetType() == obj.GetType();
        }

        public override int GetHashCode()
        {
            return this.GetType().GetHashCode();
        }
    }
}
