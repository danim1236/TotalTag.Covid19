namespace TotalTag.Covid19.GateController
{
    internal class GateControllerConfig
    {
        public int TimeoutForEntrance { get; set; } = 10;
        public int O3FlushSeconds { get; set; } = 4;
    }
}