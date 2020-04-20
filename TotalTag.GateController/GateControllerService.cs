using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using TotalTag.Common.Model;
using TotalTag.Common.Tools;
using TotalTag.Helpers;
using TotalTag.NetCore.GpioHelpers;
using TotalTag.NetCore.Service;
using TotalTag.O3.Common;
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
        private volatile bool _inProcess;
        private volatile bool _abort;

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
            if (!_inProcess && e.AssetLocationId == _facilityId)
            {
                switch (e.RouteActionType)
                {
                    case O3RouteActionTypes.START_O3_PROCESS:
                        Task.Factory.StartNew(StartO3Process);
                        break;

                    case O3RouteActionTypes.ABORT:
                        _abort = _inProcess;
                        break;

                    case O3RouteActionTypes.OPEN_BACK_DOOR:
                        Task.Factory.StartNew(StartBackdoorOpen);
                        break;
                }
            }
        }

        private void StartBackdoorOpen()
        {
            _inProcess = true;

            DoorCycle(1, false);

            _abort = false;
            _inProcess = false;
        }

        private void StartO3Process()
        {
            _inProcess = true;

            LogHelper.Write("Iniciando o processo de aspersão de O3");

            if (DoorCycle(0, true))
            {
                if (!_abort)
                {
                    LogHelper.Write($"Início da aspersão de O3 por {_config.O3FlushSeconds}s.");

                    _gpioHelper.SetPin(FLUSH_O3_PIN, true);

                    var begin = DateTime.Now;
                    while (DateTime.Now.Subtract(begin). TotalSeconds < _config.O3FlushSeconds) 
                    {
                        if (_abort)
                        {
                            break;
                        }

                        Task.Delay(100).Wait();
                    }

                    LogHelper.Write("Finalizando a aspersão de O3");

                    _gpioHelper.SetPin(FLUSH_O3_PIN, false);

                    if (!_abort)
                    {
                        DoorCycle(1, false);
                    }
                }
            }

            _abort = false;
            _inProcess = false;
        }

        private bool DoorCycle(int gate, bool useTimeout)
        {
            var openGatePin = new[] {OPEN_GATE1_PIN, OPEN_GATE2_PIN};
            var irGatePin = new[] {IR_GATE1_PIN, IR_GATE2_PIN};
            var rsGatePin = new[] {RS_GATE1_PIN, RS_GATE2_PIN};

            var fail = false;
            var door = gate + 1;

            LogHelper.Write($"Abrindo a porta {door}.");
            _gpioHelper.SetPin(openGatePin[gate], true);
            var begin = DateTime.Now;
            while (_gpioHelper.ReadPin(irGatePin[gate]))
            {
                if (_abort || (useTimeout && DateTime.Now.Subtract(begin).TotalSeconds > _config.TimeoutForEntrance))
                {
                    fail = true;
                    break;
                }
            }

            LogHelper.Write(fail
                ? $"Ninguém passou pela porta {door} em {_config.TimeoutForEntrance}s. Abortando!"
                : $"O usuário passou pela porta {door}.");

            if (!fail)
            {
                while (!_gpioHelper.ReadPin(irGatePin[gate]))
                {
                    if (_abort)
                    {
                        break;
                    }
                }
            }

            LogHelper.Write($"Fechando a porta {door}");

            _gpioHelper.SetPin(openGatePin[gate], false);

            while (!_gpioHelper.ReadPin(rsGatePin[gate]))
            {
                if (_abort)
                {
                    break;
                }
            }

            LogHelper.Write($"Porta {door} fechada.");

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

            if (!ok)
            {
                _gpioHelper = new GpioHelper();
            }

            LogHelper.Write(ok ? "Iniciando em modo Raspberry" : "Iniciando em modo simulação");
        }
    }
}