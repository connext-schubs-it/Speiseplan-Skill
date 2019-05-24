using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Alexa.NET.Request;
using Alexa.NET.Request.Type;
using Alexa.NET.Response;
using AssistServer.Extension;
using AssistServer.Models.Api.Alexa.Response;
using Essensplan.Klassen;
using Essensplan.Models;
using Essensplan.Models.Responses;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Essensplan.Controllers
{
    [Route("api/[controller]")]
    public class AlexaController : ControllerBase
    {
        private readonly string api = "https://cx-schubsit-api.azurewebsites.net/api/speiseplan/"; // Adresse der Speiseplan API
        private readonly int defaultValue = -1;

      // ##############################################################################################################
      /// <summary>
      /// Gibt Speisepläne anhand der Kalenderwoche und des Jahres zurück
      /// </summary>
      /// <param name="kw">Legt die Kalenderwoche des Jahres fest in der die Speisepläne angezeigt werden</param>
      /// <param name="year">Legt das Jahr fest, in der die Speisepläne angezeigt werden</param>
      /// <returns></returns>
      private async Task<List<SpeisePlan>> GetSpeisePlaeneNachKW(int kw, int year)
        {
            var client = new HttpClient();
            var speisePlaene = new List<SpeisePlan>();
            var path = $"{api} + kw/ + {kw}";
            var response = await client.GetAsync(path);
            if (response.IsSuccessStatusCode)
            {
                var speisePlaeneDB = JsonConvert.DeserializeObject<List<SpeisePlanDB>>(await response.Content.ReadAsStringAsync());
                speisePlaene = SpeisePlanConverter(speisePlaeneDB);
                speisePlaene = speisePlaene.FindAll(s => s.Kategorie != (int)MenueKategorien.Salat_1);
                speisePlaene = speisePlaene.FindAll(s => s.Kategorie != (int)MenueKategorien.Salat_2);
            }

            return speisePlaene;
        }

      private async Task<List<SpeisePlan>> GetSpeisePlanNachDatum(string datum)
      {
         var client = new HttpClient();
         var speisePlaen = new List<SpeisePlan>();
         var path = $"{api} + datum/ + {datum}";
         var response = await client.GetAsync(path);
         if (response.IsSuccessStatusCode)
         {
            var speisePlaeneDB = JsonConvert.DeserializeObject<List<SpeisePlanDB>>(await response.Content.ReadAsStringAsync());
            speisePlaen = SpeisePlanConverter(speisePlaeneDB);
            speisePlaen = speisePlaen.FindAll(s => s.Kategorie != (int)MenueKategorien.Salat_1);
            speisePlaen = speisePlaen.FindAll(s => s.Kategorie != (int)MenueKategorien.Salat_2);
         }
         return speisePlaen;
      }

        // ##############################################################################################################
        /// <summary>
        /// Entscheidet von welche Art die Anfrage ist und ruft die entsprechende Methode dafür auf
        /// </summary>
        /// <param name="anfrage">Enthält die Anfrage vom Amazon Alexa Server</param>
        /// <returns></returns>
        [HttpPost]
        public dynamic Alexa([FromBody]SkillRequest anfrage)
        {
            try
            {
                if (anfrage.Context.System.ApiAccessToken == null)
                    return new BadRequestResult();

                var antwort = AlexaAntwortHelfer.GibEinfacheAntwort(anfrage, SkillTypen.Error, FehlerTypen.FehlerAnfrage.ToDescription(), "", null, DateTime.Now, false);
                var requestType = anfrage.GetRequestType();

                if (requestType == typeof(LaunchRequest))
                {
                  antwort = LaunchRequestHandler(anfrage);
                }
                if(requestType == typeof(IntentRequest))
                {
                  //Intents(anfrage);
                }
                return antwort;
            }
            catch (Exception e)
            {
                CreateErrorLog(e);
                return null;
            }
        }

      private SkillResponse LaunchRequestHandler(SkillRequest anfrage)
      {
         string alexasAntwort = "Hallo Nutzer, ich helfe dir bei deinen täglichen Aufgaben.";
         return AlexaAntwortHelfer.GibEinfacheAntwort(anfrage, SkillTypen.Ended, alexasAntwort, "", null, DateTime.Now, false);
      }
      private SkillResponse IntentRequestHandler(SkillRequest anfrage)
      {
         //Intents überprüfen
         //Je nach Intent arbeiten
         var intent = (IntentRequest)anfrage.Request;
        if(intent.Intent.Name.Equals("EinfacherIntent"))
        {
            string alexasAntwort = "Die Lösung ist 3";
            return AlexaAntwortHelfer.GibEinfacheAntwort(anfrage, SkillTypen.Ended, alexasAntwort, "", null, DateTime.Now, false);
         }
        /*else if (...)
        {
        ...
        } */

         return 
      } 


      // ##############################################################################################################
      /// <summary>
      ///   Konvertiert den Anforderungen entsprechend den Speiseplan in das für Alexa nötige Format
      /// </summary>
      /// <param name="heutigeMenues">Enthält alle Speisen aus der Format für den heutigen Tag</param>
      /// <returns></returns>
      private List<SpeisePlan> SpeisePlanConverter(List<SpeisePlanDB> heutigeMenues)
      {
         var result = new List<SpeisePlan>();

         foreach (SpeisePlanDB tmp in heutigeMenues)
         {
            foreach (Gericht gericht in tmp.Gerichte)
            {
               var speise = new SpeisePlan();
               speise.Beschreibung = gericht.Bezeichnung;
               speise.Id = gericht.ID;
               var values = Enum.GetValues(typeof(MenueKategorienDB));
               foreach (MenueKategorienDB kategorieren in values)
               {
                  if (gericht.Kategorie.Equals(kategorieren.ToDescription()))
                  {
                     speise.Kategorie = kategorieren.AsInt();
                  }
               }

               speise.Preis = Convert.ToDouble(gericht.Preis);
               speise.Datum = tmp.Datum;
               result.Add(speise);
            }
         }
         return result;
      }

      // ##############################################################################################################
      /// <summary>
      /// Erzeugt eine Fehlermeldung
      /// </summary>
      /// <param name="e">Exception</param>
      private void CreateErrorLog(Exception e)
        {
            var path = @"C:\Users\gew\Documents\GitHub\Schubs_IT_Alexa\ErrorLog.txt";

            using (var writer = new StreamWriter(path, true))
            {                
                writer.WriteLine("===========Start=============");
                writer.WriteLine("Error Type: " + e.GetType().FullName);
                writer.WriteLine("Error Message: " + e.Message);
                writer.WriteLine("Stack Trace: " + e.StackTrace);
                writer.WriteLine("============End==============");
                writer.WriteLine("\n");
            }
        }
    }
}
