using Saro.Entities;
using UnityEngine;

namespace Tetris
{
    internal sealed class PieceHoldSystem : IEcsRunSystem
    {
        private readonly float m_HoldedViewPosX = -2.4f;
        private readonly float m_HoldedViewPosY = 16;
        public bool Enable { get; set; } = true;
        void IEcsRunSystem.Run(EcsSystems systems)
        {
            var world = systems.GetWorld();

            var filter = world.Filter().Inc<PieceHoldRequest>().End();
            var move = world.Filter().Inc<PieceMoveComponent>().End();
            var gameCtx = systems.GetShared<GameContext>();

            foreach (var i in filter)
            {
                ref var request = ref i.Get<PieceHoldRequest>(world);

                ref var heldPiece = ref gameCtx.heldPiece;      // 当前已经被hold的piece
                //ref var lastHeldPiece = ref m_GameCtx.lastHeldPiece;

                foreach (var i2 in move)
                {
                    if (!gameCtx.canHold) return;

                    var ePiece = world.Pack(i2);        // 要被hold的piece

                    gameCtx.canHold = false;

                    //if (heldPiece == ePiece)
                    //    return;

                    ePiece.Del<PieceMoveComponent>();
                    ePiece.Del<PieceRotateFlag>();
                    ePiece.Del<AddToGridComponent>();
                    ePiece.Del<DelayComponent>();

                    if (!heldPiece.IsAlive())
                    {
                        // 当前hold位里没有piece
                        heldPiece = ePiece;

                        gameCtx.firstHold = true;

                        TetrisUtil.ChangePieceColor(world, ref heldPiece, Color.gray);

                        gameCtx.SendMessage(new PieceNextRequest());
                    }
                    else
                    {
                        var tmpPiece = heldPiece;
                        
                        // 允许移动 & 旋转
                        // 这里要了解的知识太多了
                        tmpPiece.Add<PieceMoveComponent>();
                       
                        var pieceID = tmpPiece.Add<PieceComponent>().pieceID;
                        if (pieceID != EPieceID.O)
                            tmpPiece.Add<PieceRotateFlag>();

                        // 交换两个piece
                        heldPiece = ePiece;
                        ePiece = tmpPiece;

                        // 重置位置
                        ePiece.Add<PositionComponent>().position =
                            new Vector3(TetrisDef.Width / 2, TetrisDef.Height);
                        ePiece.Add<PieceComponent>().scale = 1f;

                        TetrisUtil.ChangePieceColor(world, ref heldPiece, Color.gray);
                        TetrisUtil.ChangePieceColor(world, ref ePiece, TetrisUtil.GetTileColor(pieceID));

                        gameCtx.SendMessage(new PieceGhostUpdateRequest { ePiece = ePiece });
                    }

                    TetrisUtil.ResetPieceRotation(world, ref heldPiece);

                    heldPiece.Add<PieceComponent>().scale = 0.6f;
                    heldPiece.Add<PositionComponent>().position = new Vector3(m_HoldedViewPosX, m_HoldedViewPosY);

                    gameCtx.SendMessage(new SeAudioEvent { audioAsset = "SE/se_game_hold.wav" });
                }
            }
        }
    }
}