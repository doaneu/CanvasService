using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CanvasService.Models.Canvas
{
    public class CommunicationChannel
    {
        public int id { get; set;}
        public string address { get; set; }
        public string type { get; set; }
        public int position { get; set; }
        public int user_id { get; set; }
        public string workflow_state { get; set; }
    }
}
