using SmartParking.MAUIHybridWebApp.KhoiDN.Shared.Services;

namespace SmartParking.MAUIHybridWebApp.KhoiDN.Web.Services
{
    public class FormFactor : IFormFactor
    {
        public string GetFormFactor()
        {
            return "Web";
        }

        public string GetPlatform()
        {
            return Environment.OSVersion.ToString();
        }
    }
}
