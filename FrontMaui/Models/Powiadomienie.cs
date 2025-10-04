using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrontMaui.Models
{
    public class Powiadomienie
    {
        public int Id { get; set; }
        public string Typ { get; set; }
        public string Opis { get; set; }
        public Szczegoly Szczegoly { get; set; }

        public string TypDisplay
        {
            get
            {
                return Typ switch
                {
                    "awaria" => "Awaria",
                    "opoznienie" => "Opóżnienie",
                    "zmiana-peronu" => "Zmiana peronu",
                    _ => Typ
                };
            }
        }

    }
}
