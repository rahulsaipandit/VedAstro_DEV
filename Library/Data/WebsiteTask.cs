using System.Xml.Linq;

namespace VedAstro.Library
{
    /// <summary>
    /// Simple data type for an internal website to-do/task item (used by the admin TaskEditor page).
    /// Reconstructed from scratch - see Library/Logic/Calculate/CoreTime.cs header note.
    /// </summary>
    public class WebsiteTask
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Status { get; set; } = "";
        public string Date { get; set; } = "";

        public XElement ToXml() => new(nameof(WebsiteTask),
            new XElement(nameof(Name), Name),
            new XElement(nameof(Description), Description),
            new XElement(nameof(Status), Status),
            new XElement(nameof(Date), Date));

        public static WebsiteTask FromXml(XElement taskXml)
        {
            string Get(string name) => taskXml.Element(name)?.Value ?? "";

            return new WebsiteTask
            {
                Name = Get(nameof(Name)),
                Description = Get(nameof(Description)),
                Status = Get(nameof(Status)),
                Date = Get(nameof(Date)),
            };
        }
    }
}
