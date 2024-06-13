using Saro.Entities;
using Saro.Entities.Extension;
using Saro;

namespace Tetris
{
    internal sealed class PieceRotateSystem : IEcsRunSystem
    {
        public bool Enable { get; set; } = true;
        void IEcsRunSystem.Run(EcsSystems systems)
        {
            var gameCtx = systems.GetShared<GameContext>();
            var world = systems.GetWorld();
            var requests = world.Filter().Inc<PieceRotationRequest>().End();
            
            // GT: TODO: 这里比较奇怪的，筛选出来的好像不只是当前的活动piece?
            // 因为涉及到wallkick，实际上约束里应该出现 PositionComponent
            var pieces = world.Filter().Inc<PieceRotateFlag, ComponentList<EcsEntity>>().End();

            // 实际只有一个PieceRotationRequest
            foreach (var i1 in requests)
            {
                ref var request = ref i1.Get<PieceRotationRequest>(world);

                foreach (var i2 in pieces)
                {
                    var ePiece = world.Pack(i2);

                    RotateBlock(world, gameCtx, ePiece, request.clockwise);
                }
            }
        }

        /// <summary>
        /// 旋转一整个piece
        /// 1. 对所有tile绕piece pivot进行旋转，不检查wallkick
        /// 2. 
        /// </summary>
        /// <param name="world"></param>
        /// <param name="ctx"></param>
        /// <param name="ePiece"></param>
        /// <param name="clockwise"></param>
        private void RotateBlock(EcsWorld world, GameContext ctx, in EcsEntity ePiece, bool clockwise)
        {
            TetrisUtil.RotateBlockWithoutCheck(world, ePiece, clockwise);

            ref var cPiece = ref ePiece.Get<PieceComponent>();
            ref var state = ref cPiece.state;
            var next = clockwise ? state + 1 : state - 1;
            if (next < 0) next = 3;
            else if (next > 3) next = 0;

            var rotateSuccess = false;
            if (!TetrisUtil.IsValidBlock(world, ctx.grid, ePiece))
            {
                // 当前旋转度无法摆放，尝试wallkick
                if (TetrisUtil.WallKickTest(world, ctx.grid, ePiece, next, out var result))
                {
                    // wallkick成功，标记rotate成功
                    Log.INFO($"wallkick: {result}");
                    state = next;

                    TetrisUtil.MovePiece(world, ctx.grid, ePiece, result);

                    rotateSuccess = true;
                }
                else // ����ʧ�ܣ���ԭ��ת
                {
                    // wallkick失败，反向旋转回归原状态
                    TetrisUtil.RotateBlockWithoutCheck(world, ePiece, !clockwise);
                    Log.INFO("wallkick failed");
                }
            }
            else
            {
                state = next;
                rotateSuccess = true;
            }

            if (rotateSuccess)
            {
                // lastOpIsRotate用于辅助判定T-spin
                ctx.lastOpIsRotate = true;

                ctx.SendMessage(new PieceRotationSuccess());

                ctx.SendMessage(new PieceGhostUpdateRequest { ePiece = ePiece });
            }

            ctx.SendMessage(new SeAudioEvent { audioAsset = "SE/se_game_rotate.wav" });
        }
    }
}