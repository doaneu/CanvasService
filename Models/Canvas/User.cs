using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CanvasService.Models.Canvas
{

    public class User
    {
        public int id { get; set; }
        public string name { get; set; }
        public DateTime created_at { get; set; }
        public string sortable_name { get; set; }
        public string short_name { get; set; }
        public string sis_user_id { get; set; }
        public string integration_id { get; set; }
        public int sis_import_id { get; set; }
        public string login_id { get; set; }
        public string pronouns { get; set; }
        public string avatar_url { get; set; }
        public string email { get; set; }
    }
}
