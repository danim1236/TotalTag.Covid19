using TotalTag.GateController.Helpers;
using TotalTag.NetCore.Service;

namespace TotalTag.GateController
{
    class Program
    {
        static void Main(string[] args)
        {
            CommonHelper.Init(true);

            new ServiceRunner<GateControllerService>().Run(args).Wait();
        }
    }
}
