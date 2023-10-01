using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriangleProject.Shared.Models.Matchix
{
    public class Pairs
    {
        public int ID { get; set; }
        public string BoardItemContect { get; set; }// קליטת שם פריט בלוח המשחק
        public string StockItemContent { get; set; }//קליטת שם מסיח במאגר
        public bool isPicBoard { get; set; }//משתנה בוליאני לבדיקת האם מדובר בתמונה בלוח המשחק
        public bool isPicStock { get; set; }// משתנה בוליאני לבדיקת האם מדובר בתמונה במאגר
        public int GameId { get; set; }
    }
    
}
