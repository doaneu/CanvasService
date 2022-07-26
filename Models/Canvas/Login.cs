using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CanvasService.Models.Canvas
{

    public class Login
    {
        public int id { get; set; }
        public int user_id { get; set; }
        public int account_id { get; set; }
        public string workflow_state { get; set; }
        public string unique_id { get; set; }
        public DateTime created_at { get; set; }
        public string sis_user_id { get; set; }
        public string integration_id { get; set; }
        public int authentication_provider_id { get; set; }
        public string declared_user_type { get; set; }
        public string authentication_provider_type { get; set; }
    }
}
