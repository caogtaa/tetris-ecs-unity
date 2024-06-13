using System.Collections.Generic;
using Saro;
using Saro.Entities;
using Saro.Entities.Extension;
using Saro.Utility;
using UnityEngine;

namespace Tetris
{
    internal sealed class PieceBagInitSystem : IEcsInitSystem
    {
        void IEcsInitSystem.Init(EcsSystems systems)
        {
            var world = systems.GetWorld();
            var ent = world.NewEntity();
            var bagList = ent.Add<ComponentList<EcsEntity>>(world).Value;
            ent.Add<PieceBagComponent>(world);

            FillBag(world, bagList, new EPieceID[7]
            {
                EPieceID.I,
                EPieceID.J,
                EPieceID.L,
                EPieceID.O,
                EPieceID.S,
                EPieceID.T,
                EPieceID.Z
            });
        }

        private void FillBag(EcsWorld world, List<EcsEntity> queue, EPieceID[] blocks)
        {
            for (var i = 0; i < blocks.Length; i++)
                queue.Add(TetrisUtil.CreatePieceForBagView(world, blocks[i], new Vector3()));
            for (var i = 0; i < blocks.Length; i++)
                queue.Add(TetrisUtil.CreatePieceForBagView(world, blocks[i], new Vector3()));

            RandomLeft(queue);
            RandomRight(queue);

            TetrisUtil.UpdateNextChainSlot(world, queue);
        }

        private static void RandomLeft(List<EcsEntity> queue)
        {
            // GT: 修正了7-bag算法
            Shuffle2(queue, 0, 7);
        }

        private static void RandomRight(List<EcsEntity> queue)
        {
            // GT: TODO: 这里怎么硬编码了
            Shuffle2(queue, 7, 7);
        }
        
        /// <summary>
        /// 代替GRandom.Shuffle()，因为取的长度有问题
        /// </summary>
        /// <param name="array"></param>
        /// <param name="start"></param>
        /// <param name="count"></param>
        /// <typeparam name="T"></typeparam>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private static void Shuffle2<T>(IList<T> array, int start, int count)
        {
            if (start < 0 || array.Count <= start || array.Count < start + count)
                throw new System.ArgumentOutOfRangeException();

            for (int i = start; i < start + count; i++)
                GRandom.Swap(array, i, GRandom.NextInt(i, start + count));
        }
    }
}