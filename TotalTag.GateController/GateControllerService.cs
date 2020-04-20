using System;
using System.Runtime.InteropServices;
using TotalTag.Common.Enums;
using TotalTag.Common.Model;
using TotalTag.Common.Tools;
using TotalTag.Helpers;
using TotalTag.NetCore.GpioHelpers;
using TotalTag.NetCore.Service;
using TotalTag.Push.Client;
using TotalTag.WebserviceClient;
using TotalTag.WebserviceClient.CoreClient;

namespace TotalTag.GateController
{
    internal class GateControllerService : MainTask
    {
        private IGpioHelper _gpioHelper;
        private readonly int _facilityId;
        private volatile bool _inO3Process;

        public GateControllerService()
        {
            _facilityId = new MachineClient().GetFacilityId();
        }
        protected override void MainThread()
        {
            InitGpio();

            IndividualEventReceiver.EnableShortcut = true;
            IndividualEventReceiver.IndividualEventTrig += OnIndividualEventTrig;
            try
            {
                PushClientFactory.Start<IndividualEventReceiver>(new TotalTagClient().Url);
            }
            catch (Exception ex)
            {
                LogHelper.Write(ex);
            }

            LogHelper.Write("Pronto");
        }

        private void OnIndividualEventTrig(object sender, IndividualEvent e)
        {
            if (!_inO3Process)
            {
                if (e.RouteActionType == RouteActionTypeEnum.AUTO_LIBERATION &&
                    e.AssetLocationId == _facilityId)
                {
                    StartO3Process();
                }
            }
        }

        private void StartO3Process()
        {
            _inO3Process = true;

            LogHelper.Write("Iniciando o processo de aspersão de O3");

            _inO3Process = false;
        }


        private void InitGpio()
        {
            bool ok;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                try
                {
                    _gpioHelper = new RaspGpioHelper(
                        new[] {16, 18, 22, 37},
                        new[] {11, 13})
                    {
                        OutPinHigLevel = PinValue.LOW
                    }.Init();

                    ok = true;
                }
                catch
                {
                    ok = false;
                }
            }
            else
            {
                ok = false;
            }

            LogHelper.Write(ok ? "Iniciando em modo Raspberry" : "Iniciando em modo simulação");
        }
    }
}