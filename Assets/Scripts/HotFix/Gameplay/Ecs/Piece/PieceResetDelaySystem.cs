using Saro.Entities;

namespace Tetris
{
    internal sealed class PieceResetDelaySystem : IEcsRunSystem
    {
        public bool Enable { get; set; } = true;
        void IEcsRunSystem.Run(EcsSystems systems)
        {
            var world = systems.GetWorld();

            var rotationSuccess = world.Filter().Inc<PieceRotationSuccess>().End();
            var moveSuccess = world.Filter().Inc<PieceMoveSuccess>().End();
            var holdRequest = world.Filter().Inc<PieceHoldRequest>().End();
            var delay = world.Filter().Inc<DelayComponent, PieceMoveComponent>().End();

            // GT: TODO: 这里的循环可读性比较差，其实都是只有一个entity
            foreach (var i in delay)
            {
                var ePiece = world.Pack(i);

                foreach (var i3 in holdRequest)
                {
                    // 刚hold过，取消AddToGrid
                    //cDelay.delay = TetrisDef.k_AddToGridDelay;
                    ePiece.Del<AddToGridComponent>();
                    ePiece.Del<DelayComponent>();
                }

                foreach (var item in moveSuccess)
                {
                    // 刚移动过，取消AddToGrid
                    //cDelay.delay = TetrisDef.k_AddToGridDelay;
                    ePiece.Del<AddToGridComponent>();
                    ePiece.Del<DelayComponent>();
                }

                foreach (var i2 in rotationSuccess)
                {
                    // 刚旋转过，取消AddToGrid
                    //cDelay.delay = TetrisDef.k_AddToGridDelay;
                    ePiece.Del<AddToGridComponent>();
                    ePiece.Del<DelayComponent>();
                }
            }
        }
    }
}