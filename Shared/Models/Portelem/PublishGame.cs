using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriangleProject.Shared.Models.Portelem
{
    public class PublishGame
    {
        public int ID { get; set; }//האיידי של המשחק שנרצה לשנות לו את הסטאטוס
        public bool IsPublish { get; set; }//המצב החדש שנרצה לבצע עבור המשחק
    }
}
