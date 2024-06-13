using Saro.Entities;

namespace Tetris
{
    public enum EDropType
    {
        Normal,
        Soft,
        Hard
    }

    public struct PieceMoveComponent : IEcsComponent
    {
        public EDropType dropType;
        public float lastFallTime;          // 上一次下落的时间，如果大于下落间隔，则要继续往下移动一格
    }
}