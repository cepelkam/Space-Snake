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

        private List<Vector2> snakeSegments = new List<Vector2>();
        private int segmentCount = 3; // 3 segmenty
        private float segmentSpacing = 20f;

        private Vector2 snakePosition;
        private Vector2 snakeVelocity;

        private List<Vector2> obstacles = new List<Vector2>();
        private List<Vector2> obstacleSizes = new List<Vector2>();
        private float minObstacleDistance = 120f; // hustší překážky

        private Vector2 portalPosition;
        private bool portalActive = false;

        private float scrollSpeed = 4f; // startovní rychlost
        private float timeSinceStart = 0f;
        private float portalDelay = 2f; // čas od poslední překážky do portálu

        private List<Vector2> stars = new List<Vector2>();
        private int starCount = 200;
        private Random rand = new Random();

        private int score = 0;

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

            // had fixní v rovině
            for (int i = 0; i < segmentCount; i++)
                snakeSegments.Add(snakePosition - new Vector2(i * segmentSpacing, 0));

            // hvězdy
            for (int i = 0; i < starCount; i++)
                stars.Add(new Vector2(rand.Next(0, 800), rand.Next(0, 600)));

            // spawn překážek
            SpawnInitialObstacles();

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

        private void SpawnInitialObstacles()
        {
            float x = 400;
            while (x < 2500) // víc překážek dopředu
            {
                float y = rand.Next(50, 550);
                float size = rand.Next(30, 50);
                obstacles.Add(new Vector2(x, y));
                obstacleSizes.Add(new Vector2(size, size));
                x += minObstacleDistance + rand.Next(30, 100);
            }

            // portal za poslední překážkou + malá prodleva
            portalPosition = new Vector2(x + scrollSpeed * portalDelay * 60, 250); // fixní Y pro portal
        }

        protected override void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            timeSinceStart += dt;

            scrollSpeed = 4f + timeSinceStart * 0.2f;

            var kstate = Keyboard.GetState();

            // pohyb had nahoru/dolů, ale segmenty fixní
            snakeVelocity = Vector2.Zero;
            if (kstate.IsKeyDown(Keys.Up)) snakeVelocity.Y = -200f;
            if (kstate.IsKeyDown(Keys.Down)) snakeVelocity.Y = 200f;

            snakePosition += snakeVelocity * dt;
            snakePosition.Y = MathHelper.Clamp(snakePosition.Y, 0, _graphics.PreferredBackBufferHeight - 20);

            for (int i = 0; i < segmentCount; i++)
                snakeSegments[i] = snakePosition - new Vector2(i * segmentSpacing, 0); // fixní segmenty

            // posun překážek
            for (int i = 0; i < obstacles.Count; i++)
                obstacles[i] -= new Vector2(scrollSpeed, 0);

            // aktivace portálu, když dojedeš poslední překážku
            if (!portalActive && obstacles.Count == 0)
                portalActive = true;

            if (portalActive)
                portalPosition.X -= scrollSpeed;

            // odstranění překážek mimo obrazovku a skore
            for (int i = obstacles.Count - 1; i >= 0; i--)
            {
                if (obstacles[i].X + obstacleSizes[i].X < 0)
                {
                    obstacles.RemoveAt(i);
                    obstacleSizes.RemoveAt(i);
                    score++;
                }
            }

            // kolize s překážkami
            Rectangle headRect = new Rectangle((int)snakeSegments[0].X, (int)snakeSegments[0].Y, 20, 20);
            for (int i = 0; i < obstacles.Count; i++)
            {
                Rectangle obsRect = new Rectangle((int)obstacles[i].X, (int)obstacles[i].Y, (int)obstacleSizes[i].X, (int)obstacleSizes[i].Y);
                if (headRect.Intersects(obsRect))
                {
                    ResetLevel();
                    return;
                }
            }

            // kolize s portálem
            if (portalActive)
            {
                Rectangle portalRect = new Rectangle((int)portalPosition.X, (int)portalPosition.Y, 50, 50);
                if (headRect.Intersects(portalRect))
                {
                    ResetLevel(); // restart hry po dojetí portálu
                }
            }

            base.Update(gameTime);
        }

        private void ResetLevel()
        {
            snakePosition = new Vector2(100, 300);
            for (int i = 0; i < segmentCount; i++)
                snakeSegments[i] = snakePosition - new Vector2(i * segmentSpacing, 0);

            obstacles.Clear();
            obstacleSizes.Clear();
            SpawnInitialObstacles();

            portalActive = false;
            timeSinceStart = 0f;
            scrollSpeed = 4f;
            score = 0;
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            _spriteBatch.Begin();

            // hvězdy
            foreach (var star in stars)
                _spriteBatch.Draw(starTexture, new Rectangle((int)star.X, (int)star.Y, 2, 2), Color.White);

            // překážky
            for (int i = 0; i < obstacles.Count; i++)
                _spriteBatch.Draw(obstacleTexture, new Rectangle((int)obstacles[i].X, (int)obstacles[i].Y, (int)obstacleSizes[i].X, (int)obstacleSizes[i].Y), Color.Red);

            // portál
            if (portalActive)
                _spriteBatch.Draw(portalTexture, new Rectangle((int)portalPosition.X, (int)portalPosition.Y, 50, 50), Color.Cyan);

            // had
            foreach (var seg in snakeSegments)
                _spriteBatch.Draw(snakeSegmentTexture, new Rectangle((int)seg.X, (int)seg.Y, 20, 20), Color.LimeGreen);

            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
