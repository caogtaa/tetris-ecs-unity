#if UNITY_EDITOR
#define ENABLE_DEBUG_ECS
#endif

using Saro;
using Saro.Entities;
using UnityEngine;

namespace Tetris
{
    public sealed class TetrisStartup : MonoBehaviour
    {
        public GameplayAssets gameplayAssets;
        public BatchRenderer batchRenderer;

#if ENABLE_DEBUG_ECS
        private EcsSystems m_EditorSystems;
#endif
        private EcsSystems m_Systems;

        private async void Start()
        {
            var world = new EcsWorld("Game");

            var gameCtx = new GameContext(world);
            gameCtx.batchRenderer = batchRenderer;
            gameCtx.gameplayAssets = gameplayAssets;

            // TODO 可能要个预载画面
            await gameplayAssets.PreloadAssets();

            // GT: GameContext作为所有system之间共享的数据
            m_Systems = new EcsSystems("GameSystems", world, gameCtx);

            m_Systems
                .Add(new DelaySystem())

                // logic
                // 用于游戏计时更新
                .Add(new GameTimeSystem())
                
                // init中实例化视图(tetrisBoard)，包括背景板、全局特效、网格线
                // 生产GameStartRequest
                .Add(new BoardCreateSystem())
                
                // 初始化2个7-bag，初始化bag -> piece -> tile 层级结构，前5个pieces指定位置、颜色用于预览
                .Add(new PieceBagInitSystem())
                
                // 检测用户输入，触发相应的移动、旋转、hold请求
                // 生产 PieceDropRequest、PieceMoveRequest、PieceRotationRequest、PieceHoldRequest
                .Add(new GameInputSystem())
                
                // 消费 GameStartRequest
                // 生产 PieceNextRequest、BGMAudioEvent
                // 打开分数UI
                .Add(new GameStartSystem())
                
                // 消费PieceRotationRequest
                // 生产PieceRotationSuccess、PieceGhostUpdateRequest、SeAudioEvent
                // 做wallkick测试
                .Add(new PieceRotateSystem())
                
                // 消费PieceDropRequest、PieceMoveRequest
                // 生产PieceMoveSuccess、PieceGhostUpdateRequest、SeAudioEvent
                // 增加AddToGridComponent、DelayComponent
                // 处理左右移动、各种着陆。着陆后增加AddToGridComponent，延迟处理
                // 有一个疑问是当判定着陆后，可能因为wallkick或者侧移重新回到未着陆状态，这个时候AddToGridComponent应该移除了
                .Add(new PieceMoveSystem())
                
                // 消费PieceHoldRequest
                // 生产PieceNextRequest、PieceGhostUpdateRequest、SeAudioEvent
                // 交换当前piece和held piece，重置tile位置。特殊处理第一次hold
                .Add(new PieceHoldSystem())
                
                // 消费PieceRotationSuccess、PieceMoveSuccess、PieceHoldRequest
                // 移除AddToGridComponent、DelayComponent
                // 一次成功的位移、hold后，推迟AddToGrid
                .Add(new PieceResetDelaySystem())
                
                // 消费AddToGridComponent
                // 生产SeAudioEvent、EffectEvent、LineClearRequest
                // 增加新entity with GameEndComponent + DelayComponent
                // 判定game over，修改grids[][]内容
                .Add(new AddToGridSystem())
                
                // 消费LineClearRequest、LineClearDelayRequest
                // 生产LineClearDelayRequest、PieceGhostUpdateRequest、PieceNextRequest
                // 广播TetrisScoreEventArgs、TetrisLineClearArgs
                // 判定T-spin、计分
                .Add(new LineClearSystem())
                
                // 消费PieceNextRequest
                // 生产PieceSpawnRequest
                .Add(new PieceNextSystem())

                // 消费PieceSpawnRequest
                // 生产GameEndComponent + DelayComponent、PieceGhostUpdateRequest
                .Add(new PieceSpawnSystem())
                
                // 消费GameEndComponent
                // 展示ending ui，销毁界面
                .Add(new GameEndSystem())

                // 新增，entity的清理函数
                .Add(new EntityDestroyFeature())

                // view
                // 消费PieceGhostUpdateRequest
                .Add(new PieceGhostSystem())
                
                // 搜集所有TileRenderer，组Mesh，提交
                .Add(new TileRendererSystem())
                
                // 消费SeAudioEvent、BGMAudioEvent
                .Add(new AudioSystem())
                
                // 消费EffectEvent
                .Add(new EffectSystem());

            // GT: 这里的用意应该是每帧结束前(所有业务system运行完毕后)，将所有临时的requests组件清理干净
            m_Systems
                // one frame
                .Del<GameStartRequest>()
                .Del<PieceNextRequest>()
                .Del<PieceSpawnRequest>()
                .Del<LineClearRequest>()
                .Del<PieceRotationRequest>()
                .Del<PieceMoveRequest>()
                .Del<PieceDropRequest>()
                .Del<PieceHoldRequest>()
                .Del<PieceRotationSuccess>()
                .Del<PieceMoveSuccess>()
                .Del<PieceGhostUpdateRequest>()
                .Del<SeAudioEvent>()
                .Del<BGMAudioEvent>()
                .Del<EffectEvent>();

            m_Systems
                .Init();

#if ENABLE_DEBUG_ECS
            m_EditorSystems = new EcsSystems("DebugSystem", world);
            m_EditorSystems
                .Add(new Saro.Entities.UnityEditor.EcsWorldDebugSystem(world))
                .Init();
#endif
        }

        private void Update()
        {
            m_Systems?.Run();
        }

#if ENABLE_DEBUG_ECS
        private void LateUpdate()
        {
            m_EditorSystems?.Run();
        }
#endif

        private void OnDestroy()
        {
            if (m_Systems != null)
            {
                m_Systems.Destroy();
                m_Systems.GetWorld()?.Destroy();
                m_Systems = null;
            }
        }
    }
}