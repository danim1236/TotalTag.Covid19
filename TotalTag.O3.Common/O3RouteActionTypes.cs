using TotalTag.Common.Enums;

namespace TotalTag.O3.Common
{
    public static class O3RouteActionTypes
    {
        public const RouteActionTypeEnum START_O3_PROCESS = RouteActionTypeEnum.AUTO_LIBERATION;
        public const RouteActionTypeEnum ABORT = RouteActionTypeEnum.AUTO_EMERGENCY_CALL;
        public const RouteActionTypeEnum OPEN_BACK_DOOR = RouteActionTypeEnum.MANUAL_LIBERATION;
    }
}
