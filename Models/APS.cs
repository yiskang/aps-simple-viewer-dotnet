using Autodesk.SDKManager;

public partial class APS
{
    private readonly SDKManager _sdkManager;
    private readonly string _clientId;
    private readonly string _clientSecret;

    public string ClientId
    {
        get
        {
            return _clientId;
        }
    }

    public APS(string clientId, string clientSecret, string bucket = null)
    {
        _sdkManager = SdkManagerBuilder.Create().Build();
        _clientId = clientId;
        _clientSecret = clientSecret;
    }
}
