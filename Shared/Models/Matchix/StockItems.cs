using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriangleProject.Shared.Models.Matchix
{
    public class StockItems
    {
        public int BId { get; set; } //קישור לפריט במאגר-מפתח זר
        public int StockID { get; set; } //מפתח ראשי של פריט בסטוק
        public bool isPic { get; set; } //האם הפריט נבחר?
        public string StockContent { get; set; } //תוכן הפריט
        
    }
}
