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

        public GameController(GameModel model, GameView view)
        {
            _model = model;
            _view = view;
        }

        public void HandleInput(KeyboardState kb, MouseState mouse)
        {
            if (kb.IsKeyDown(Keys.Tab) && !_prev.IsKeyDown(Keys.Tab))
                _model.GodMode = !_model.GodMode;

            _prev = kb;

            bool actionPressed = kb.IsKeyDown(Keys.Space) || mouse.LeftButton == ButtonState.Pressed;

            switch (_model.State)
            {
                case GameState.Menu:
                    if (actionPressed) _view.StartNewGame();
                    break;
                case GameState.Playing:
                    if (actionPressed) _view.Jump();
                    break;
                case GameState.GameOver:
                    if (actionPressed) _view.GoToMenu();
                    break;
            }
        }
    }
}
