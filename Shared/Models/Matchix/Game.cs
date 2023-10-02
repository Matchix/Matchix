using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriangleProject.Shared.Models.Unity
{
    public class Game
    {
        public int ID { get; set; }
        public int UserId {get; set;}
        public int GameCode { get; set; }
        public string GameName { get; set; }
        public double PairCount { get; set; }
        public bool canPublic { get; set; }
        public bool IsPublish { get; set; }
    }
}
