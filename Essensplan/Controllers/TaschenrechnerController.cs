using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Alexa.NET;
using Alexa.NET.Request;
using Alexa.NET.Request.Type;
using Alexa.NET.Response;
using AssistServer.Extension;
using AssistServer.Extension.NewFolder;
using AssistServer.Models.Api.Alexa.Response;
using Essensplan.Klassen;
using Essensplan.Models;
using Essensplan.Models.Responses;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Essensplan.Controllers
{
    [Route("api/[controller]")]
    public class TaschenrechnerController : ControllerBase
    {
        private readonly int defaultValue = -1;

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

                //Ab hier wird geprüft, welche Art von Anfrage gemacht wird

                if (requestType == typeof(LaunchRequest))
                {
                    //Tritt nur auf, wenn man den Skill startet

                    string bildschirmText = "Hallo. Mit mir kannst du wie mit einem Taschenrechner rechnen";
                    string ueberschrift = "Taschenrechner";
                    string sprachausgabe = bildschirmText;

                    return AlexaAntwortHelfer.GibEinfacheAntwort(anfrage, SkillTypen.Willkommen, bildschirmText, ueberschrift, sprachausgabe, DateTime.Now, false);
                }
                else if(requestType == typeof(IntentRequest))
                {
                    var intentAnfrage = (IntentRequest)anfrage.Request;

                    if (intentAnfrage.Intent.Name.Equals("")) //<-- Hier der Name von deinem Intent, z.B. BerechnenIntent
                    {
                        //Tritt nur auf, wenn der entsprechende Intent gestartet wird
                        //Wiederholt sich solange, bis 2 Zahlen und die entsprechende Rechenoperation genannt wurden

                        var intentRequest = (IntentRequest)anfrage.Request;

                        if (intentRequest.DialogState.Equals("STARTED"))
                        {
                            return ResponseBuilder.DialogDelegate(anfrage.Session, intentRequest.Intent);
                        }
                        else if (!intentRequest.DialogState.Equals("COMPLETED"))
                        {
                            return ResponseBuilder.DialogDelegate(anfrage.Session);
                        }
                        else
                        {
                            antwort = BerechnenHelfer(anfrage);
                        }
                    }                        
                }

                return antwort;
            }
            catch (Exception e)
            {
                CreateErrorLog(e);
                return null;
            }
        }    
        
        public SkillResponse BerechnenHelfer(SkillRequest anfrage)
        {
            var intentAnfrage = (IntentRequest)anfrage.Request;
            int zahl1 = anfrage.GetSlotValueInt(SlotValues.Zahl_1.ToString(), defaultValue);
            int zahl2 = 0;
            int operation = 0;

            /*  definiere ein Array, dass folgende Wert beinhaltet: 1, 2, 3, 4
              
                In der Anfrage muss herausgefunden werden, welche Operation gemacht werden soll
                
                wenn 1, dann +
                wenn 2, dann -
                wenn 3, dann *
                wenn 4, dann /

                aus zahl1, zahl2 und operation soll das ergebnis geformt werden                
            */


            //Diese Werte müssen definiert werden
            string bildschirmText = "";
            string ueberschrift = "";
            string sprachausgabe = "";

            return AlexaAntwortHelfer.GibEinfacheAntwort(anfrage, SkillTypen.Berechnen, bildschirmText, ueberschrift, sprachausgabe, DateTime.Now, false);
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
