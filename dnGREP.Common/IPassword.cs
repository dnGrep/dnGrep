namespace dnGREP.Common
{
    public interface IPassword
    {
        string RequestPassword(string subject, string details, bool isRetry);
    }
}
