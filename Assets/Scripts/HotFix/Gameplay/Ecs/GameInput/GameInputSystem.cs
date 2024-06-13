//#define USE_INPUT_HUD

using Saro.Entities;
using Saro.Entities.Extension;
using Saro.UI;
using Tetris.UI;
using UnityEngine;

namespace Tetris
{
    internal sealed class GameInputSystem : IEcsRunSystem, IEcsInitSystem, IEcsDestroySystem
    {
        public bool Enable { get; set; } = true;

        private IInputController m_InputController;

        void IEcsInitSystem.Init(EcsSystems systems)
        {
            var world = systems.GetWorld();
            // GT: GameInputComponent用于存储当前所有输入的状态
            // 实际的输入由Input_Keyboard来检测
            world.GetSingleton<GameInputComponent>();

#if USE_INPUT_HUD
            m_InputController = new Input_HUD();
#else
            m_InputController = new Input_Keyboard();
#endif

            // GT: 会从world里区GameInputComponent进行状态设置
            // 不得不说ECS越过了接口的约定，让组件的共享变得更加随意。这种随意反而需要逻辑上的耦合，并且容易出现纰漏
            m_InputController.BindInput(world);
        }

        void IEcsRunSystem.Run(EcsSystems systems)
        {
            var world = systems.GetWorld();
            var dt = Time.deltaTime;
            ref var input = ref world.GetSingleton<GameInputComponent>();
            ProcessBlockInput(world, ref input, dt);
        }

        /// <summary>
        /// GameInputComponent包含两组状态
        /// 1. 检测的玩家输入状态
        /// 2. 根据玩家输入状态标记的待处理临时状态，并结合上一帧的临时状态发送最终状态变化的请求
        /// </summary>
        /// <param name="world"></param>
        /// <param name="input"></param>
        /// <param name="deltaTime"></param>
        private void ProcessBlockInput(EcsWorld world, ref GameInputComponent input, float deltaTime)
        {
            m_InputController.ProcessInput();   // 实际访问input并且设置GameInputComponent状态

            // hard & soft drop
            if (input.spaceDown)
            {
                // SendMessage本质上是新建一个entity到world里面并挂在消息体组件。对应的system进行filter并处理
                // 注意帧末尾要删除所有消息体组件（框架的约定是最后一个组件被删除时，entity自动被删除）
                // 这里隐式约定了消息必须在当前帧被处理，否则下一帧就拿不到消息了
                // PieceDropRequest的处理函数是PieceMoveSystem
                world.SendMessage(new PieceDropRequest { dropType = EDropType.Hard });
            }
            else if (!input.downPressed && input.downArrowDown)
            {
                // 进入soft下落模式，实测只对当前piece有效，不知道是哪里做了状态清除
                // soft下落模式和其他操作都兼容，可以同时输入。所以作为一种normal模式的加速版本来使用
                input.downPressed = true;
                world.SendMessage(new PieceDropRequest { dropType = EDropType.Soft });
            }
            else if (input.downPressed && input.downArrowUp)
            {
                // 退出soft下落模式
                input.downPressed = false;
                world.SendMessage(new PieceDropRequest { dropType = EDropType.Normal });
            }

            // move left
            if (input.leftArrowDown)
            {
                // 仅首次按压会进入，立即触发一次移动
                world.SendMessage(new PieceMoveRequest { moveDelta = Vector2.left });
                input.leftPressed = true;
            }
            if (!input.rightPressed && input.leftPressed && input.leftArrowPressing)
            {
                // 持续按压
                if (input.lastStartTime >= TetrisDef.StartTime)
                {
                    // lastStartTime初始值为0，表示持续按压时间。超过StartTime时进入快速侧移状态
                    if (input.lastInputTime >= TetrisDef.InputDelta)
                    {
                        world.SendMessage(new PieceMoveRequest { moveDelta = Vector2.left });
                        input.lastInputTime = 0;
                    }
                    else
                    {
                        input.lastInputTime += deltaTime;
                    }
                }
                else
                {
                    input.lastStartTime += deltaTime;
                }
            }
            if (input.leftPressed && input.leftArrowUp)
            {
                // 释放按钮
                input.leftPressed = false;
                input.lastStartTime = 0;
                input.lastInputTime = 0;
            }

            // move right
            if (input.rightArrowDown)
            {
                world.SendMessage(new PieceMoveRequest { moveDelta = Vector2.right });
                input.rightPressed = true;
            }
            if (!input.leftPressed && input.rightPressed && input.rightArrowPressing)
            {
                if (input.lastStartTime >= TetrisDef.StartTime)
                {
                    if (input.lastInputTime >= TetrisDef.InputDelta)
                    {
                        world.SendMessage(new PieceMoveRequest { moveDelta = Vector2.right });

                        input.lastInputTime = 0;
                    }
                    else
                    {
                        input.lastInputTime += deltaTime;
                    }
                }
                else
                {
                    input.lastStartTime += deltaTime;
                }
            }
            if (input.rightPressed && input.rightArrowUp)
            {
                input.rightPressed = false;
                input.lastStartTime = 0;
                input.lastInputTime = 0;
            }

            // rotate
            if (input.zDown) world.SendMessage(new PieceRotationRequest { clockwise = false });
            if (input.xDown) world.SendMessage(new PieceRotationRequest { clockwise = true });

            // hold
            if (input.cDown) world.SendMessage(new PieceHoldRequest());
        }

        void IEcsDestroySystem.Destroy(EcsSystems systems)
        {
            m_InputController.OnDestroy();
        }
    }
}