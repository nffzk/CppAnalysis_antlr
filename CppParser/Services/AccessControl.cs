namespace CppParser.Services
{
    public sealed class AccessControl
    {
        public string Current { get; private set; } = "private";

        public void EnterClass(string stereotype /* class|struct|union */)
        {
            Current = stereotype == "class" ? "private" : "public";
        }

        public void LeaveClass()
        {
            Current = "private";
        }

        public void Set(string accessSpecifier)
        {
            switch (accessSpecifier)
            {
                case "public": Current = "public"; break;
                case "protected": Current = "protected"; break;
                case "private": Current = "private"; break;
            }
        }
    }
}
