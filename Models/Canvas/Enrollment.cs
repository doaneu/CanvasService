using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CanvasService.Models.Canvas
{

    public class Enrollment
    {
        public int id { get; set; }
        public string sis_section_id { get; set; }
        public int course_id { get; set; }
        public string enrollment_state { get; set; }
        public int course_section_id { get; set; }
        public int role_id { get; set; }
    }
}
