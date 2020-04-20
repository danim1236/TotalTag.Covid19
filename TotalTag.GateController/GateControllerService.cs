using System;
using TotalTag.NetCore.Service;

namespace TotalTag.GateController
{
    internal class GateControllerService : MainTask
    {
        protected override void MainThread()
        {
            Console.WriteLine("Executando a Thread Principal");
        }
    }
}