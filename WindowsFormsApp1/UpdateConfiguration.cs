using System.Configuration;

namespace WindowsFormsApp1
{
    public class UpdateConfiguration
    {
        public string FeedUrl { get; set; }

        public bool IsConfigured
        {
            get { return !string.IsNullOrWhiteSpace(FeedUrl); }
        }

        public static UpdateConfiguration Load()
        {
            return new UpdateConfiguration
            {
                FeedUrl = ConfigurationManager.AppSettings["VelopackFeedUrl"]
            };
        }
    }
}
