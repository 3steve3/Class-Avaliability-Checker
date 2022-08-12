using HtmlAgilityPack;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace ClassChecker
{
    public class IsOpen
    {
        public string SeatsAvail { get; set; } = "";
        string? seats;
        [JsonPropertyName("seats")]
        public string? Seats
        {
            get => seats; set
            {
                //remove seats variable later
                seats = value;
                if (string.IsNullOrEmpty(value))
                    return;
                var doc = new HtmlDocument();
                doc.LoadHtml(value);
                var html = doc.DocumentNode.SelectSingleNode("/").GetDirectInnerText().Replace("/", "");
                var split = html.Split(':');
                if (split.Length >= 3)
                {
                    SeatsAvail = split[2].TrimStart();
                }
            }
        }
    }
    public class ClassChecker
    {
        async static void SendEmail(string link)
        {
            string apiKey = "My_Key";
            var client = new SendGridClient(apiKey);
            var from_email = new EmailAddress("email@colorado.edu", "First Last");
            var subject = "A space opened up in a class";
            var to_email = new EmailAddress("email@colorado.edu", "First Last");
            var plainTextContent = $"Check the Class here: {link}";
            var htmlContent = "";
            var msg = MailHelper.CreateSingleEmail(from_email, to_email, subject, plainTextContent, htmlContent);
            await client.SendEmailAsync(msg).ConfigureAwait(false);
        }
        static readonly HttpClient httpClient = new();
        async static Task<bool> IsOpen(string crn, string term)
        {
            var content = new StringContent("{\"key\":\"crn: " + crn + "\",\"srcdb\":\"" + term + "\",\"matched\":\"crn: " + crn + "\"}");
            var response = await httpClient.PostAsync("https://classes.colorado.edu/api/?page=fose&route=details", content).ConfigureAwait(false);
            var details = await response.Content.ReadFromJsonAsync<IsOpen>().ConfigureAwait(false);
#pragma warning disable CA1806 // Default Value is checked in next line if conversion fails
            int.TryParse(details?.SeatsAvail, out int result);
#pragma warning restore CA1806 // Default Value is checked in next line if conversion fails
            if (result > 0)
                return true;
            return false;
        }
        static async Task Main()
        {
            //set the class crn here
            
            string ClassCRN = "";
            while (string.IsNullOrEmpty(ClassCRN))
            {
                Console.WriteLine("Enter the class CRN for the class to check");
                ClassCRN = Console.ReadLine();
            }
            //set the term here
            string Term = "";
            while (string.IsNullOrEmpty(Term))
            {
                Console.WriteLine("Enter the term as a 4 digit code such as 2227 for Fall 2022");
                Term = Console.ReadLine();
            }
            while (true)
            {
                if (await IsOpen(ClassCRN, Term))
                    SendEmail($"https://classes.colorado.edu/?srcdb={Term}&keyword={ClassCRN}");
                else
                    Console.WriteLine("No Changes");
                //sleep for 2 minutes
                await Task.Delay(120000);
            }
        }
    }
}