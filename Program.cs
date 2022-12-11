using CanvasService.Tasks;
using Conductor.Client.Extensions;
using Conductor.Client.Interfaces;

namespace CanvasService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            IHost host = Host.CreateDefaultBuilder(args)
                .ConfigureServices(services =>
                {
                    services.AddConductorWorker(ConductorConnection.Instance.Configuration);

                    //Add Tasks here
                    services.AddConductorWorkflowTask<CheckCanvasUserExistsTask>();
                    services.AddConductorWorkflowTask<CreateCanvasUserTask>();
                    services.AddConductorWorkflowTask<UpdateCanvasUserTask>();
                    services.AddConductorWorkflowTask<DeleteCanvasUserTask>();

                    services.AddHostedService<WorkflowsWorkerService>();
                })
                .Build();

            host.Run();
        }
    }

    internal class WorkflowsWorkerService : BackgroundService
    {
        private readonly IWorkflowTaskCoordinator workflowTaskCoordinator;
        private readonly IEnumerable<IWorkflowTask> workflowTasks;

        public WorkflowsWorkerService(
            IWorkflowTaskCoordinator workflowTaskCoordinator,
            IEnumerable<IWorkflowTask> workflowTasks
        )
        {
            this.workflowTaskCoordinator = workflowTaskCoordinator;
            this.workflowTasks = workflowTasks;
        }

        protected override async System.Threading.Tasks.Task ExecuteAsync(CancellationToken stoppingToken)
        {
            foreach (var worker in workflowTasks)
            {
                workflowTaskCoordinator.RegisterWorker(worker);
            }
            // start all the workers so that it can poll for the tasks
            await workflowTaskCoordinator.Start();
        }
    }
}
