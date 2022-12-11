using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Conductor.Api;
using Conductor.Client;

namespace CanvasService
{
    public class ConductorConnection
    {
        private static ConductorConnection instance;
        public Configuration Configuration { get; private set; }
        public MetadataResourceApi MetadataResourceApi { get; private set; }
        public EventResourceApi EventResourceApi { get; private set; }

        private ConductorConnection() { }

        public static ConductorConnection Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new ConductorConnection();

                    instance.Configuration = new Configuration(new ConcurrentDictionary<string, string>(), null, null, "http://localhost:8080/");
                    MetadataResourceApi metadataResourceApi = new MetadataResourceApi(instance.Configuration);

                    try
                    {
                        int count = metadataResourceApi.GetAllWorkflows().Count;
                    }
                    catch
                    {
                        instance.Configuration = new Configuration(new ConcurrentDictionary<string, string>(), null, null, "http://conductor-server-svc:8080/");
                    }

                    instance.MetadataResourceApi = new MetadataResourceApi(instance.Configuration);
                    instance.EventResourceApi = new EventResourceApi(instance.Configuration);

                }

                return instance;
            }
        }
    }

    //public class ConductorConnection
    //{
    //    private static ConductorConnection instance;

    //    public Configuration Configuration { get; private set; }
    //    public MetadataResourceApi MetadataResourceApi { get; private set; }
    //    public EventResourceApi EventResourceApi { get; private set; }


    //    // Explicit static constructor to tell C# compiler
    //    // not to mark type as beforefieldinit
    //    static ConductorConnection()
    //    {  
    //    }

    //    private ConductorConnection()
    //    {
    //    }

    //    public static ConductorConnection Instance
    //    {
    //        get
    //        {
    //            if(instance.Configuration == null)
    //            {
    //                instance.Configuration = new Configuration(new ConcurrentDictionary<string, string>(), null, null, "http://localhost:8080/");
    //                MetadataResourceApi metadataResourceApi = new MetadataResourceApi(instance.Configuration);

    //                try
    //                {
    //                    int count = metadataResourceApi.GetAllWorkflows().Count;
    //                }
    //                catch
    //                {
    //                    instance.Configuration = new Configuration(new ConcurrentDictionary<string, string>(), null, null, "http://conductor-server-svc:8080/");
    //                }

    //                instance.MetadataResourceApi = new MetadataResourceApi(instance.Configuration);
    //                instance.EventResourceApi = new EventResourceApi(instance.Configuration);
    //            }

    //            return instance;
    //        }      
    //    }
    //}
}
