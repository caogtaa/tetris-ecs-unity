using Saro.Entities;
using Saro.Entities.Extension;
using UnityEngine;

namespace Tetris
{
    internal sealed class PieceMoveSystem : IEcsRunSystem
    {
        public const float DeltaNormal = 1f;
        public bool Enable { get; set; } = true;
        void IEcsRunSystem.Run(EcsSystems systems)
        {
            var gameCtx = systems.GetShared<GameContext>();
            var grid = gameCtx.grid;

            var world = systems.GetWorld();

            var dropRequests = world.Filter().Inc<PieceDropRequest>().End();
            var moveRequests = world.Filter().Inc<PieceMoveRequest>().End();
            var moves = world.Filter().Inc<PieceMoveComponent, ComponentList<EcsEntity>, PositionComponent>()
                .End();

            var deltaTime = Time.deltaTime;

            foreach (var i in moves)
            {
                var ePiece = world.Pack(i);
                ref var cMove = ref i.Get<PieceMoveComponent>(world);

                foreach (var i2 in dropRequests)
                {
                    ref var request = ref i2.Get<PieceDropRequest>(world);

                    cMove.dropType = request.dropType;

                    if (cMove.dropType == EDropType.Soft) {
                        // 软着陆时重置下落时间
                        // TODO: 有个bug，如果快速敲击向下按钮，piece会悬浮在空中
                        cMove.lastFallTime = 0f;
                    } else if (cMove.dropType == EDropType.Hard) {
                        // 硬着陆时不认定T-spin?
                        gameCtx.lastOpIsRotate = false;
                    }
                }

                foreach (var i1 in moveRequests)
                {
                    ref var request = ref i1.Get<PieceMoveRequest>(world);

                    var moved = TetrisUtil.MovePiece(world, grid, ePiece, request.moveDelta);

                    if (moved)
                    {
                        gameCtx.lastOpIsRotate = false;

                        gameCtx.SendMessage(new PieceMoveSuccess());

                        gameCtx.SendMessage(new PieceGhostUpdateRequest { ePiece = ePiece });

                        gameCtx.SendMessage(new SeAudioEvent { audioAsset = "SE/se_game_move.wav" });
                    }
                }

                AutoDrop(world, grid, ePiece, deltaTime, Vector2.down);
            }
        }

        private void AutoDrop(EcsWorld world, EcsEntity[][] grid, in EcsEntity ePiece, float deltaTime,
            in Vector2 moveDelta)
        {
            ref var cMove = ref ePiece.Get<PieceMoveComponent>();

            ref var lastFallTime = ref cMove.lastFallTime;

            float dropDeltaTime;        // 下落间隔。默认1s一次，软着陆时70ms一次，硬着陆？
            switch (cMove.dropType)
            {
                case EDropType.Normal:
                default:
                    dropDeltaTime = DeltaNormal;
                    break;
                case EDropType.Soft:
                    dropDeltaTime = DeltaNormal * 0.07f;
                    break;
                case EDropType.Hard:
                    dropDeltaTime = 0f;
                    break;
            }

            lastFallTime += deltaTime;

            // 硬着陆时因为dropDeltaTime = 0，这里会死循环直到方块着陆
            while (lastFallTime >= dropDeltaTime)
            {
                lastFallTime -= dropDeltaTime;
                if (!TetrisUtil.MovePiece(world, grid, ePiece, moveDelta))
                {
                    if (ePiece.Has<AddToGridComponent>() == false) ePiece.Add<AddToGridComponent>();
                    if (cMove.dropType != EDropType.Hard) {
                        if (ePiece.Has<DelayComponent>() == false) {
                            ref var delay = ref ePiece.Add<DelayComponent>();
                            delay.delay = TetrisDef.AddToGridDelay;
                        }
                    }

                    lastFallTime = 0f;
                    break;
                }
            }
        }
    }
}