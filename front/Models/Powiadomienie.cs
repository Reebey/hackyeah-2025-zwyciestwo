using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace front.Models
{
    public class Powiadomienie
    {
        public int Id { get; set; }
        public string Typ { get; set; } = string.Empty;
        public string Opis { get; set; } = string.Empty;
        public Szczegoly Szczegoly { get; set; } = new();

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
