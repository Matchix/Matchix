using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriangleProject.Shared.Models.Matchix
{
    public class BoardItems
    {
        public int Gcode { get; set; } //קוד המשחק-מפתח זר
        public int BoardID { get; set; } //מפתח ראשי-מקושר לטבלת פריטי הסטוק
        public bool isPic { get; set; } //האם הפריט נבחר?
        public string BoardContent { get; set; } //תוכן הפריט

    }
}
