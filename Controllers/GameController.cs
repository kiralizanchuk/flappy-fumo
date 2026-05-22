using Microsoft.Xna.Framework.Input;
using FumoGame.Models;
using FumoGame.Views;

namespace FumoGame.Controllers
{
    public class GameController
    {
        private readonly GameModel _model;
        private readonly GameView _view;
        private KeyboardState _prev;
        private bool _prevMouseDown;

        // Двойной клик для рывка
        private int _dblClickCount  = 0;
        private int _dblClickFrames = 0;
        private const int DblClickWindow = 18; // ~0.3с при 60 fps

        public GameController(GameModel model, GameView view)
        {
            _model = model;
            _view = view;
        }

        public void HandleInput(KeyboardState kb, MouseState mouse)
        {
            bool actionDown        = kb.IsKeyDown(Keys.Space) || mouse.LeftButton == ButtonState.Pressed;
            bool actionJustPressed = actionDown && !_prevMouseDown;

            if (kb.IsKeyDown(Keys.Tab) && !_prev.IsKeyDown(Keys.Tab))
                _model.GodMode = !_model.GodMode;

            // Счётчик двойного клика
            _dblClickFrames++;
            if (_dblClickFrames > DblClickWindow)
            {
                _dblClickCount  = 0;
                _dblClickFrames = 0;
            }
            if (actionJustPressed)
            {
                _dblClickCount++;
                _dblClickFrames = 0;
                if (_dblClickCount >= 2 && _model.State == GameState.Playing)
                {
                    _model.DashRequested = true;
                    _dblClickCount  = 0;
                    _dblClickFrames = 0;
                }
            }

            switch (_model.State)
            {
                case GameState.Menu:
                    if (actionJustPressed) _view.HandleMenuClick(mouse.X, mouse.Y);
                    break;
                case GameState.Playing:
                    if (actionDown) _view.Jump();
                    break;
                case GameState.GameOver:
                    if (actionJustPressed) _view.GoToMenu();
                    break;
                case GameState.MusicSelect:
                    if (actionJustPressed) _view.HandleMusicSelectClick(mouse.X, mouse.Y);
                    break;
            }

            _prev = kb;
            _prevMouseDown = actionDown;
        }
    }
}
