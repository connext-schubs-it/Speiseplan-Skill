using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Alexa.NET.Request;
using Alexa.NET.Request.Type;
using Alexa.NET.Response;
using Alexa.NET.Response.Directive;
using AssistServer.Extension;
using AssistServer.Extension.NewFolder;
using AssistServer.Models.Api.Alexa.Response;
using Essensplan.Extensions;
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
        private async Task<List<SpeisePlan>> GetSpeisePlaeneNachKW(int kw)
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
                    antwort = StartVerwalter(anfrage);
                }
                if (requestType == typeof(IntentRequest))
                {
                    antwort = KommandoVerwalter(anfrage);
                }
                else if (requestType == typeof(SessionEndedRequest))
                {
                    antwort = SitzungBeendenVerwalter(anfrage);
                }



                return antwort;
            }
            catch (Exception e)
            {
                CreateErrorLog(e);
                return null;
            }
        }

        // ##############################################################################################################
        /// <summary>
        /// Entscheidet welche Art des Kommandos gesprochen wurde
        /// </summary>
        /// <param name="anfrage">Enthält die Anfrage vom Amazon Alexa Server vom Typ Kommando</param>
        /// <returns></returns>
        private SkillResponse KommandoVerwalter(SkillRequest anfrage)
        {
            var antwort = AlexaAntwortHelfer.GibEinfacheAntwort(anfrage, SkillTypen.Error, FehlerTypen.FehlerAnfrage.ToDescription(), "", null, DateTime.Now, false);
            var intentRequest = (IntentRequest)anfrage.Request;

            if (intentRequest.Intent.Name.Equals("")) //<-- Hier der Name von deinem Intent, z.B. HeutigerSpeisePlan
                antwort = HeutigerSpeisePlanHelfer(anfrage);

            return antwort;
        }

        private SkillResponse HeutigerSpeisePlanHelfer(SkillRequest anfrage)
        {
            DateTime heutigerTtag = DateTime.Now; //<-- Hiermit wird der heutige Tag ermittelt
            DateTime tag = new DateTime(2000, 12, 31); //<-- Das ist der 31.12.2000
            int kw = tag.GetWeekOfYear(); //<-- ermittelt die KW des Datums


            List<SpeisePlan> speisePlanTag = GetSpeisePlanNachDatum(tag.ToString()).Result;  //<--- Sucht den Speiseplan nach dem angegebenen Datum
            List<SpeisePlan> speisePlanWoche = GetSpeisePlaeneNachKW(kw).Result; //<-- sucht den Speiseplan aus der entsprechenden Woche


            //skillTyp: einen eindeutigen Namen geben
            //text: Was angezeigt werden soll
            //title: Überschrift
            //speech: was gesagt wird

            return AlexaAntwortHelfer.GibEinfacheAntwort(anfrage, skilltyp, text, title, speech, DateTime.Now, false);
        }

        private SkillResponse HeutigerSpeisePlanHelferListe(SkillRequest anfrage)
        {
            DateTime heutigerTtag = DateTime.Now; //<-- Hiermit wird der heutige Tag ermittelt
            DateTime tag = new DateTime(2000, 12, 31); //<-- Das ist der 31.12.2000
            int kw = tag.GetWeekOfYear(); //<-- ermittelt die KW des Datums


            List<SpeisePlan> speisePlanTag = GetSpeisePlanNachDatum(tag.ToString()).Result;  //<--- Sucht den Speiseplan nach dem angegebenen Datum
            List<SpeisePlan> speisePlanWoche = GetSpeisePlaeneNachKW(kw).Result; //<-- sucht den Speiseplan aus der entsprechenden Woche


            List<ListItem> items = new List<ListItem>();
            ListItem item = AlexaAntwortHelfer.ErstelleListenEintrag(skillTyp, text1, text2, text3, bild); //<-- so wird ein Eintrag für die Listenansicht erstellt. Die Information findet man im Spieseplan
            items.Add(item);


            IOutputSpeech speech = new SsmlOutputSpeech() //<-- Hiermit wird die Sprachausgabe generiert
            {
                Ssml = ""
            };

            //title: Überschrift
            //skillTyp: eindeutiger Name, kann der gleiche Name wie oben benutzt werden

            return AlexaAntwortHelfer.ErstelleListenAnsicht(anfrage, skillTyp, items, speech, defaultCard, title, DateTime.Now, false);
        }












        // ##############################################################################################################
        /// <summary>
        /// Enthält die Antwort des Alexa Skills welche beim Starten des Skills gegeben wird
        /// </summary>
        /// <param name="anfrage">Enthält die Anfrage vom Amazon Alexa Server vom Typ Start</param>
        /// <returns></returns>
        private SkillResponse StartVerwalter(SkillRequest anfrage)
        {
            string text = "Herzlich Willkommen!";
            string title = "Connext Campus";
            string speech = "Willkommen beim Connext Campus. Was kann ich für Sie tun?";
            return AlexaAntwortHelfer.GibEinfacheAntwort(anfrage, SkillTypen.Willkommen, text, title, speech, DateTime.Now, false);
        }

        // ##############################################################################################################
        /// <summary>
        /// Regelt die Antwort beim Beenden des Alexa Skills
        /// </summary>
        /// <param name="request">Enthält die Anfrage vom Amazon Alexa Server vom Typ SitzungBeenden</param>
        /// <returns></returns>
        private SkillResponse SitzungBeendenVerwalter(SkillRequest request)
        {
            return AlexaAntwortHelfer.GibEinfacheAntwort(request, SkillTypen.Ended, AlexaAntwortHelfer.Ended, FehlerTypen.Ended.ToDescription(), null, DateTime.Now, true);
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

        private ICard defaultCard()
        {
            return new SimpleCard()
            {
                Title = "Liste",
                Content = "Liste"
            };
        }
    }
}
