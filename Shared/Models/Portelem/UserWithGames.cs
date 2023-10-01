using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriangleProject.Shared.Models.Unity;


namespace TriangleProject.Shared.Models.Portelem
{
    public class UserWithGames
    {
        public string FirstName { get; set; }
        public int ID { get; set; }
        public List<Game> Games { get; set; }

    }
}
