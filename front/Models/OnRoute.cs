using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace front.Models
{
    public class OnRoute
    {
        public List<Candidate> candidates { get; set; }
    }

    public class Candidate
    {
        public string routeId { get; set; }
        public string routeShortName { get; set; }
        public double distanceMeters { get; set; }
    }     
}
