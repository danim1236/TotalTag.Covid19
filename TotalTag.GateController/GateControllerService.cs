using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
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
        private GateControllerConfig _config;
        private IGpioHelper _gpioHelper;
        private readonly int _facilityId;
        private volatile bool _inO3Process;

        private const uint OPEN_GATE1_PIN = 0;
        private const uint OPEN_GATE2_PIN = 1;
        private const uint FLUSH_O3_PIN = 2;

        private const uint IR_GATE1_PIN = 0;
        private const uint RS_GATE1_PIN = 1;
        private const uint IR_GATE2_PIN = 2;
        private const uint RS_GATE2_PIN = 3;

        public GateControllerService()
        {
            _facilityId = new MachineClient().GetFacilityId();
        }

        protected override void MainThread()
        {
            _config = new MachineConfigClient().GetTypedMachineConfig<GateControllerConfig>();

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
            if (!_inO3Process &&
                e.RouteActionType == RouteActionTypeEnum.AUTO_LIBERATION && e.AssetLocationId == _facilityId)
            {
                Task.Factory.StartNew(StartO3Process);
            }
        }

        private void StartO3Process()
        {
            _inO3Process = true;

            LogHelper.Write("Iniciando o processo de aspersão de O3");

            if (DoorCycle(0, true))
            {

                _gpioHelper.SetPin(FLUSH_O3_PIN, true);

                Task.Delay(_config.O3FlushSeconds * 1000).Wait();

                _gpioHelper.SetPin(FLUSH_O3_PIN, false);

                DoorCycle(1, false);
            }

            _inO3Process = false;
        }

        private bool DoorCycle(int gate, bool useTimeout)
        {
            var openGatePin = new[] {OPEN_GATE1_PIN, OPEN_GATE2_PIN};
            var irGatePin = new[] {IR_GATE1_PIN, IR_GATE2_PIN};
            var rsGatePin = new[] {RS_GATE1_PIN, RS_GATE2_PIN};

            var fail = false;
            _gpioHelper.SetPin(openGatePin[gate], true);
            var begin = DateTime.Now;
            while (_gpioHelper.ReadPin(irGatePin[gate]))
            {
                if (useTimeout && DateTime.Now.Subtract(begin).TotalSeconds > _config.TimeoutForEntrance)
                {
                    fail = true;
                    break;
                }
            }

            if (!fail)
            {
                while (!_gpioHelper.ReadPin(irGatePin[gate]))
                {
                }
            }

            _gpioHelper.SetPin(openGatePin[gate], false);

            while (!_gpioHelper.ReadPin(rsGatePin[gate]))
            {
            }

            return !fail;
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
                        new[] {11, 13, 15})
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