using Microsoft.Bot.Builder;
using Microsoft.BotBuilderSamples.Clu;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace CoreBot.CognitiveModels
{
    public class AutoGarageBotModel : IRecognizerConvert
    {
        public enum Intent
        {
            MakeAppointment,
            OpeningHours,
            RepairType,
            None
        }

        public string Text { get; set; }

        public string AlteredText { get; set; }

        public Dictionary<Intent, IntentScore> Intents { get; set; }

        public CluEntities Entities { get; set; }

        public IDictionary<string, object> Properties { get; set; }

        public void Convert(dynamic result)
        {
            var jsonResult = JsonConvert.SerializeObject(result, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            var app = JsonConvert.DeserializeObject<AutoGarageBotModel>(jsonResult);

            Text = app.Text;
            AlteredText = app.AlteredText;
            Intents = app.Intents;
            Entities = app.Entities;
            Properties = app.Properties;
        }

        public (Intent intent, double score) GetTopIntent()
        {
            var maxIntent = Intent.None;
            var max = 0.0;
            foreach (var entry in Intents)
            {
                if (entry.Value.Score > max)
                {
                    maxIntent = entry.Key;
                    max = entry.Value.Score.Value;
                }
            }

            return (maxIntent, max);
        }

        public class CluEntities
        {
            public CluEntity[] Entities;

            public string GetFirstName() => Entities.Where(e => e.Category == "FirstName").ToArray().FirstOrDefault()?.Text;
            public string GetAppointmentDate() => Entities.Where(e => e.Category == "AppointmentDate").ToArray().FirstOrDefault()?.Text;
            public string GetLastName() => Entities.Where(e => e.Category == "LastName").ToArray().FirstOrDefault()?.Text;
            public string GetLicensePlate() => Entities.Where(e => e.Category == "LicensePlate").ToArray().FirstOrDefault()?.Text;
            public string GetMail() => Entities.Where(e => e.Category == "Mail").ToArray().FirstOrDefault()?.Text;
            public string GetPhoneNumber() => Entities.Where(e => e.Category == "PhoneNumber").ToArray().FirstOrDefault()?.Text;
            public string GetRepairType() => Entities.Where(e => e.Category == "RepairType").ToArray().FirstOrDefault()?.Text;
            public string GetTimeSlot() => Entities.Where(e => e.Category == "TimeSlot").ToArray().FirstOrDefault()?.Text;

        }
    }

}
