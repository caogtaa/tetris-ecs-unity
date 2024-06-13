using Saro.Entities;

namespace Tetris
{
    public struct GameStartRequest : IEcsComponent
    {
        public int gameMode;    // GT: 目前用不到

        public override string ToString()
        {
            return $"{nameof(GameStartRequest)}=[{gameMode}]";
        }
    }
}