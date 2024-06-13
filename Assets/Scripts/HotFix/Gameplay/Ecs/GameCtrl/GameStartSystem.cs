using Saro.Entities;
using Saro.UI;
using Tetris.UI;

namespace Tetris
{
    internal sealed class GameStartSystem : IEcsRunSystem
    {
        private GameContext m_GameCtx;
        public bool Enable { get; set; } = true;
        void IEcsRunSystem.Run(EcsSystems systems)
        {
            var world = systems.GetWorld();
            m_GameCtx = systems.GetShared<GameContext>();
            
            // 消费GameStartRequest。实际上每局游戏只会触发一次这个request
            var gameStartRequests = world.Filter().Inc<GameStartRequest>().End();
            foreach (var start in gameStartRequests)
            {
                // 实际上bag只有一个
                var bags = world.Filter().Inc<PieceBagComponent>().End();

                var gameStartRequest = start.Get<GameStartRequest>(world);

                // GameContext发送消息，和World直接发送消息等效
                foreach (var ii in bags) m_GameCtx.SendMessage(new PieceNextRequest());

                m_GameCtx.gamming = true;
                m_GameCtx.SendMessage(new BGMAudioEvent { audioAsset = "BGM/bgm_t02_swap_t.wav" });

                OpenUI();
            }
        }

        private async void OpenUI()
        {
            await UIManager.Current.LoadAndShowWindowAsync(EGameUI.UIGameHUD, m_GameCtx);
        }
    }
}