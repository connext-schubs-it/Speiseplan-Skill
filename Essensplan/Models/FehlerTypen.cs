using System.ComponentModel;

namespace Essensplan.Models.Responses
{
    public enum FehlerTypen
    {
        [Description("Fehler aufgetreten")] Fehler = 3,
        [Description("Ihre Anfrage konnte nicht verarbeitet werden")] FehlerAnfrage = 4,
        [Description("Speiseplan beendet")] Ended = 5,
    }
}
