using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriangleProject.Shared.Models.Matchix
{
    public class Games
    {
        public int GameCode { get; set; }
        public string GameName { get; set; }
        public bool isPublic { get; set; }
        public double PairCount { get; set; }
        public double GameTime { get; set; }
        public int UserId { get; set; }

    }
}
