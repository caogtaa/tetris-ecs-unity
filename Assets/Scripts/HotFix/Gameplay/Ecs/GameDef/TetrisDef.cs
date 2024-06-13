namespace Tetris
{
    public class TetrisDef
    {
        public const int Height = 20;
        public const int Width = 10;
        public const int ExtraHeight = 2;

        public const float DeltaNormal = 1f;
        public const float DeltaSoft = 0.03f;

        public const float InputDelta = 0.03f;      // 连续移动的间隔时间，30ms移动一格
        public const float StartTime = 0.15f;       // 侧移键触发连续移动的启动间隔

        public const float LineClearDelay = 0.5f;
        public const float AddToGridDelay = 0.4f;
    }
}