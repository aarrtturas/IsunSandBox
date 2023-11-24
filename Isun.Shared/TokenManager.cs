namespace Isun.Shared;
public sealed class TokenManager
{
#pragma warning disable CS8618
    private static TokenManager _instance;

    private string token;

    private TokenManager() { }

    public static TokenManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new TokenManager();
            }
            return _instance;
        }
    }

    public string Token
    {
        get { return token; }
        set { token = value; }
    }
#pragma warning restore CS8618
}
