using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace Space_Snake
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private Texture2D snakeSegmentTexture;
        private Texture2D obstacleTexture;
        private Texture2D portalTexture;
        private Texture2D starTexture;

        private Vector2 snakePosition;
        private List<Vector2> obstacles = new List<Vector2>();
        private List<Vector2> obstacleSizes = new List<Vector2>();
        private Vector2 portalPosition;
        private float scrollSpeed = 6f; // rychlejší scroll
        private Random rand = new Random();

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            _graphics.PreferredBackBufferWidth = 800;
            _graphics.PreferredBackBufferHeight = 600;
            _graphics.ApplyChanges();

            snakePosition = new Vector2(100, 300);

            // jednoduché překážky
            for (int i = 0; i < 5; i++)
            {
                float y = rand.Next(50, 550);
                obstacles.Add(new Vector2(400 + i * 200, y));
                obstacleSizes.Add(new Vector2(50, 50));
            }

            // portál
            portalPosition = new Vector2(800, 250);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            snakeSegmentTexture = new Texture2D(GraphicsDevice, 1, 1);
            snakeSegmentTexture.SetData(new[] { Color.LimeGreen });

            obstacleTexture = new Texture2D(GraphicsDevice, 1, 1);
            obstacleTexture.SetData(new[] { Color.Red });

            portalTexture = new Texture2D(GraphicsDevice, 1, 1);
            portalTexture.SetData(new[] { Color.Cyan });

            starTexture = new Texture2D(GraphicsDevice, 1, 1);
            starTexture.SetData(new[] { Color.White });
        }

        protected override void Update(GameTime gameTime)
        {
            var kstate = Keyboard.GetState();

            // pohyb nahoru/dolů
            if (kstate.IsKeyDown(Keys.Up)) snakePosition.Y -= 200f * (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (kstate.IsKeyDown(Keys.Down)) snakePosition.Y += 200f * (float)gameTime.ElapsedGameTime.TotalSeconds;
            snakePosition.Y = MathHelper.Clamp(snakePosition.Y, 0, _graphics.PreferredBackBufferHeight - 20);

            // posun překážek a portálu
            for (int i = 0; i < obstacles.Count; i++)
                obstacles[i] -= new Vector2(scrollSpeed, 0);

            portalPosition.X -= scrollSpeed;

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            _spriteBatch.Begin();

            // hvězdy
            for (int i = 0; i < 200; i++)
            {
                _spriteBatch.Draw(starTexture, new Rectangle(rand.Next(0, 800), rand.Next(0, 600), 2, 2), Color.White);
            }

            // překážky
            for (int i = 0; i < obstacles.Count; i++)
                _spriteBatch.Draw(obstacleTexture, new Rectangle((int)obstacles[i].X, (int)obstacles[i].Y, (int)obstacleSizes[i].X, (int)obstacleSizes[i].Y), Color.Red);

            // portál
            _spriteBatch.Draw(portalTexture, new Rectangle((int)portalPosition.X, (int)portalPosition.Y, 50, 50), Color.Cyan);

            // had
            _spriteBatch.Draw(snakeSegmentTexture, new Rectangle((int)snakePosition.X, (int)snakePosition.Y, 20, 20), Color.LimeGreen);

            _spriteBatch.End();
            base.Draw(gameTime);
        }
    }
}
