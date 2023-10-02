using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriangleProject.Shared.Models.Matchix
{
    public class PortelemUser
    {
        public int ID { get; set; } // האיי די שמתקבל כשמתחברים למשחק
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int PortelemId { get; set; } // האיי די שמקבלים כשמתחברים לפורט"למ

    }
}
