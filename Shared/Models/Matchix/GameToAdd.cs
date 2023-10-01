using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriangleProject.Shared.Models.Matchix
{
    public class GameToAdd
    {
        public string GameName { get; set; } = "";
        //GameToAdd gameToAdd = new GameToAdd();//לבדוק אם זה נכון

        public int TimeForQ { get; set; } //זמן לשאלה
        public string Instruction { get; set; }


    }
}
