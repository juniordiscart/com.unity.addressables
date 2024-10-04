namespace AddressableAssets.DocExampleCode
{
    // A replacement used in documentation to change an attribute typer without changing its
    // by stating an attribute such as MenuItem to not actually be MenuItem but DummyAttribute (using MenuItem = DummyAttribute)
    // Doing this allows the naming remain, but effects of the attribute to not hold, allowing the documentation to display the correct naming.
    internal class DummyAttribute : System.Attribute
    {
        public DummyAttribute()
        {
        }

        public DummyAttribute(string parameter)
        {
        }
    }
}
