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

        public GameController(GameModel model, GameView view)
        {
            _model = model;
            _view = view;
        }

        public void HandleInput(KeyboardState kb, MouseState mouse)
        {
            bool actionDown = kb.IsKeyDown(Keys.Space) || mouse.LeftButton == ButtonState.Pressed;
            bool actionJustPressed = actionDown && !_prevMouseDown;

            if (kb.IsKeyDown(Keys.Tab) && !_prev.IsKeyDown(Keys.Tab))
                _model.GodMode = !_model.GodMode;

            switch (_model.State)
            {
                case GameState.Menu:
                    if (actionJustPressed) _view.StartNewGame();
                    break;
                case GameState.Playing:
                    if (actionDown) _view.Jump();
                    break;
                case GameState.GameOver:
                    if (actionJustPressed) _view.GoToMenu();
                    break;
            }

            _prev = kb;
            _prevMouseDown = actionDown;
        }
    }
}
