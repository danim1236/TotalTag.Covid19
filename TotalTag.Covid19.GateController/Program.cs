using TotalTag.Covid19.GateController.Helpers;
using TotalTag.NetCore.Service;

namespace TotalTag.Covid19.GateController
{
    class Program
    {
        static void Main(string[] args)
        {
            CommonHelper.Init();

            new ServiceRunner<GateControllerService>().Run(args).Wait();
        }
    }
}
