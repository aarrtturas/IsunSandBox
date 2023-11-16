namespace Isun.Shared;
public class ArgsManager
{
#pragma warning disable CS8618
    private static ArgsManager _instance;

    private string cities;
    private string password;

    private ArgsManager() { }

    public static ArgsManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new ArgsManager();
            }
            return _instance;
        }
    }

    public string Cities
    {
        get { return cities; }
        set { cities = value; }
    }

    public string Password
    {
        get { return password; }
        set { password = value; }
    }
#pragma warning restore CS8618
}
