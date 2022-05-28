using Leopotam.EcsLite;
using Leopotam.EcsLite.Extension;
using UnityEngine;

namespace Tetris
{
    internal sealed class PieceMoveSystem : IEcsRunSystem
    {
        public const float DeltaNormal = 1f;

        void IEcsRunSystem.Run(EcsSystems systems)
        {
            var gameCtx = systems.GetShared<GameContext>();
            var grid = gameCtx.grid;

            var world = systems.GetWorld();

            var dropRequests = world.Filter().Inc<PieceDropRequest>().End();
            var moveRequests = world.Filter().Inc<PieceMoveRequest>().End();
            var moves = world.Filter().Inc<PieceMoveComponent, ComponentList<EcsPackedEntity>, PositionComponent>()
                .End();

            var deltaTime = Time.deltaTime;

            foreach (var i in moves)
            {
                var ePiece = world.PackEntity(i);
                ref var cMove = ref i.Get<PieceMoveComponent>(world);

                foreach (var i2 in dropRequests)
                {
                    ref var request = ref i2.Get<PieceDropRequest>(world);

                    cMove.dropType = request.dropType;
                    if (cMove.dropType == EDropType.Soft)
                        cMove.lastFallTime = 0f;
                    else if (cMove.dropType == EDropType.Hard) gameCtx.lastOpIsRotate = false;
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

        private void AutoDrop(EcsWorld world, EcsPackedEntity[][] grid, in EcsPackedEntity ePiece, float deltaTime,
            in Vector2 moveDelta)
        {
            ref var cMove = ref ePiece.Get<PieceMoveComponent>(world);

            ref var lastFallTime = ref cMove.lastFallTime;

            float dropDeltaTime;
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

            while (lastFallTime >= dropDeltaTime)
            {
                lastFallTime -= dropDeltaTime;
                if (!TetrisUtil.MovePiece(world, grid, ePiece, moveDelta))
                {
                    if (ePiece.Has<AddToGridComponent>(world) == false) ePiece.Add<AddToGridComponent>(world);
                    if (cMove.dropType != EDropType.Hard)
                        if (ePiece.Has<DelayComponent>(world) == false)
                        {
                            ref var delay = ref ePiece.Add<DelayComponent>(world);
                            delay.delay = TetrisDef.AddToGridDelay;
                        }

                    lastFallTime = 0f;
                    break;
                }
            }
        }
    }
}