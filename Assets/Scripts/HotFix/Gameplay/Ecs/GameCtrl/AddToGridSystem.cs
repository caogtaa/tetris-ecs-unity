using Saro.Entities;
using Saro.Entities.Extension;
using Saro;
using UnityEngine;

namespace Tetris
{
    internal sealed class AddToGridSystem : IEcsRunSystem
    {
        private GameContext m_GameCtx;
        private EcsEntity[][] Grid => m_GameCtx.grid;

        public bool Enable { get; set; } = true;
        void IEcsRunSystem.Run(EcsSystems systems)
        {
            m_GameCtx = systems.GetShared<GameContext>();

            var world = systems.GetWorld();

            // 没有Delay组件了，但是有AddToGrid，说明延迟到期了，需要立刻AddToGrid
            var pieces = world.Filter().Inc<PieceMoveComponent, ComponentList<EcsEntity>, AddToGridComponent>()
                .Exc<DelayComponent>()
                .End();

            //foreach (var i1 in m_Requests)
            {
                foreach (var ePiece in pieces)
                {
                    ref var cPiecePosition = ref ePiece.Get<PositionComponent>(world);
                    var tileList = ePiece.Get<ComponentList<EcsEntity>>(world).Value;

                    var isGameOver = true;

                    var yMin = int.MaxValue;
                    var yMax = 0;

                    for (var i = 0; i < tileList.Count; i++)
                    {
                        var eTile = tileList[i];
                        ref var cTilePos = ref eTile.Get<PositionComponent>();
                        var pos = cTilePos.position + cPiecePosition.position;
                        var x = Mathf.RoundToInt(pos.x);
                        var y = Mathf.RoundToInt(pos.y);

                        if (y < TetrisDef.Height) {
                            // 只要有1个tile进入了20以内的格子，就没有GameOver
                            // 上面预留了2个高度用于spawn，注意如果改为18-bag的话，这个高度也要做相应调整
                            // 注意到长条必须是横着出来的，如果竖着出来占3个格子，有可能会和当前的判定逻辑冲突，导致过早暴毙
                            // 注意顶部同样参与wallkick
                            isGameOver = false;
                        }

                        Grid[y][x] = eTile;     // GameOver的情况下可能会覆盖格子

                        if (y > yMax) yMax = y;
                        if (y < yMin) yMin = y;
                    }

                    ref var cMove = ref ePiece.Get<PieceMoveComponent>(world);
                    FireSound(cMove.dropType);
                    FireEffect(cMove.dropType, ePiece.Get<PositionComponent>(world).position);

                    {
                        //ePiece.Del<PieceComponent>(world);
                        ePiece.Del<PieceMoveComponent>(world);
                        ePiece.Del<PieceRotateFlag>(world);
                        ePiece.Del<AddToGridComponent>(world);
                    }

                    // check game over
                    if (isGameOver)
                    {
                        m_GameCtx.SendMessage(new GameEndComponent(), new DelayComponent { delay = 1f });

                        Log.ERROR("GameOver");
                    }
                    else
                    {
                        m_GameCtx.SendMessage(
                            new LineClearRequest
                                { ePiece = world.Pack(ePiece), startLine = yMin, endLine = yMax });
                    }
                }
            }
        }

        private void FireEffect(EDropType dropType, Vector3 position)
        {
            if (dropType == EDropType.Hard)
                m_GameCtx.SendMessage(new EffectEvent
                    { effectAsset = "vfx_hard_drop.prefab", effectPosition = position });
        }

        private void FireSound(EDropType dropType)
        {
            switch (dropType)
            {
                case EDropType.Normal:
                    m_GameCtx.SendMessage(new SeAudioEvent { audioAsset = "SE/se_game_landing.wav" });
                    break;
                case EDropType.Soft:
                    m_GameCtx.SendMessage(new SeAudioEvent { audioAsset = "SE/se_game_softdrop.wav" });
                    break;
                case EDropType.Hard:
                    m_GameCtx.SendMessage(new SeAudioEvent { audioAsset = "SE/se_game_harddrop.wav" });
                    break;
            }
        }
    }
}