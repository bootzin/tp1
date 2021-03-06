﻿using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace TP1
{
	public class Game : GameWindow
	{
		public GameState State { get; private set; }
		private bool shouldPause, showInfo, drawInstruction;
		private string[] lines = new string[] { };

		public List<GameLevel> Levels { get; set; } = new List<GameLevel>();
		public int Level { get; set; }

		public GameObject Player { get; set; }
		private Vector2 PLAYER_SIZE = new Vector2(150, 20);
		private float PLAYER_VELOCITY = 8f;
		private int Lives;
		private int score = 0;
		public Difficulty Difficulty { get; set; }
		public List<Difficulty> Difficulties { get; set; } = new List<Difficulty>();

		public List<BallObject> Balls { get; set; } = new List<BallObject>();
		public BallObject Ball { get; set; }
		private readonly float BALL_RADIUS = 12.5f;
		private const float BALL_X_SPEED = 8;

		public ParticleGenerator ParticleGenerator { get; set; }
		public PostProcessor PostProcessor { get; set; }
		private float shakeTime;

		public TextRenderer TextRenderer { get; set; }
		public Texture2D Overlay { get; set; }

		public List<PowerUp> PowerUps { get; set; } = new List<PowerUp>();

		public Renderer2D Renderer { get; private set; }

		public Game(int width, int height, string title) : base(width, height, GraphicsMode.Default, title, GameWindowFlags.FixedWindow)
		{
			// iniciar a viewport e habilitar o uso de "alpha" para determinação de opacidade
			GL.Viewport(0, 0, width, height);
			GL.Enable(EnableCap.Blend);
			GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

			// determinar as dificuldades do jogo
			Difficulties.Add(new Difficulty("Facil", -12f, .65, 3, 3, 5)
			{
				PowerUpTypes = new List<string>()
				{
					"speed", "sticky", "comet", "padIncrease", "padDecrease", "multiply",
					"speed", "sticky", "comet", "padIncrease", "padDecrease", "multiply",
				},
				PowerUpDuration = new Dictionary<string, float>()
				{
					{"speed", 8},{"sticky", 10},{"comet", 6},{"padIncrease", 12},{"padDecrease", 5},{"confuse", 8},{"chaos", 3},{ "multiply", 0.00001f}
				}
			});
			Difficulties.Add(new Difficulty("Medio", -14f, .45, 3, 2, 3)
			{
				PowerUpTypes = new List<string>()
				{
					"speed", "sticky", "comet", "padIncrease", "padDecrease", "multiply", "chaos",
					"speed", "sticky", "comet", "padIncrease", "padDecrease", "confuse",
				},
				PowerUpDuration = new Dictionary<string, float>()
				{
					{"speed", 10},{"sticky", 8},{"comet", 5},{"padIncrease", 10},{"padDecrease", 8},{"confuse", 6},{"chaos", 4},{ "multiply", 0.00001f}
				}
			});
			Difficulties.Add(new Difficulty("Dificil", -16f, .25, 2, 0, 2)
			{
				PowerUpTypes = new List<string>()
				{
					"speed", "sticky", "comet", "padIncrease", "padDecrease", "chaos", "confuse",
					"speed", "sticky", "comet", "padIncrease", "padDecrease", "chaos", "confuse",
				},
				PowerUpDuration = new Dictionary<string, float>()
				{
					{"speed", 12},{"sticky", 6},{"comet", 3},{"padIncrease", 10},{"padDecrease", 8},{"confuse", 8},{"chaos", 5},{ "multiply", 0f}
				}
			});

			//dificuldade inicial é média
			Difficulty = Difficulties[1];

			Init();
		}

		//Método responsável por inicializar todos os recursos utilizados no jogo
		public void Init()
		{
			// carregar sons
			var bgm = ResourceManager.LoadSound("resources/bgm.mp3", "bgm", true);
			SoundEngine.Instance.PlaySound(bgm);

			ResourceManager.LoadSound("resources/bleep.wav", "bleepPaddle");
			ResourceManager.LoadSound("resources/powerup.wav", "powerup");
			ResourceManager.LoadSound("resources/solid.wav", "solid");
			ResourceManager.LoadSound("resources/bleep.mp3", "bleepBrick");

			//carregar shaders
			ResourceManager.LoadShader("shaders/sprite.vert", "shaders/sprite.frag", "sprite");
			ResourceManager.LoadShader("shaders/particle.vert", "shaders/particle.frag", "particle");
			ResourceManager.LoadShader("shaders/postProcessing.vert", "shaders/postProcessing.frag", "postProcessing");

			//instanciar a matriz de projeção ortogonal
			var projection = Matrix4.CreateOrthographicOffCenter(0, Width, Height, 0, -1, 1);

			// instanciar o renderizador padrão de texturas
			var spriteshader = ResourceManager.GetShader("sprite");
			var particleshader = ResourceManager.GetShader("particle");
			spriteshader.SetInteger("image", 0, true);
			spriteshader.SetMatrix4("projection", projection, true);
			particleshader.SetInteger("sprite", 0, true);
			particleshader.SetMatrix4("projection", projection, true);
			Renderer = new Renderer2D(spriteshader);

			// carregar as texturas do jogo
			ResourceManager.LoadTexture("resources/ball.png", "ball");
			ResourceManager.LoadTexture("resources/paddle.png", "paddle");
			ResourceManager.LoadTexture("resources/block.png", "block", false);
			ResourceManager.LoadTexture("resources/block_solid.png", "block_solid", false);
			ResourceManager.LoadTexture("resources/background.jpg", "breakout_bg", false);

			ResourceManager.LoadTexture("resources/particle.png", "particle");

			ResourceManager.LoadTexture("resources/speed.png", "speed");
			ResourceManager.LoadTexture("resources/meteor.png", "comet");
			ResourceManager.LoadTexture("resources/width.png", "padIncrease");
			ResourceManager.LoadTexture("resources/shrink.png", "padDecrease");
			ResourceManager.LoadTexture("resources/multiply.png", "multiply");
			ResourceManager.LoadTexture("resources/sticky.png", "sticky");
			ResourceManager.LoadTexture("resources/confuse.png", "confuse");
			ResourceManager.LoadTexture("resources/skull.png", "chaos");

			//carregar os níveis disponíveis no jogo
			GameLevel one = new GameLevel();
			one.Load("levels/one.lvl", Width, Height / 2);
			GameLevel two = new GameLevel();
			two.Load("levels/two.lvl", Width, Height / 2);
			GameLevel three = new GameLevel();
			three.Load("levels/three.lvl", Width, Height / 2);
			GameLevel four = new GameLevel();
			four.Load("levels/four.lvl", Width, Height / 2);
			GameLevel five = new GameLevel();
			five.Load("levels/five.lvl", Width, Height / 2);
			GameLevel six = new GameLevel();
			six.Load("levels/six.lvl", Width, Height / 2);
			Levels.Add(one);
			Levels.Add(two);
			Levels.Add(three);
			Levels.Add(four);
			Levels.Add(five);
			Levels.Add(six);
			Level = 4;

			// instanciar jogador
			const float offset = 84f;
			Vector2 playerStartPos = new Vector2((Width / 2) - (PLAYER_SIZE.X / 2), Height - PLAYER_SIZE.Y - offset);
			Player = new GameObject(playerStartPos, PLAYER_SIZE, ResourceManager.GetTex("paddle"));

			// instanciar bola
			Vector2 ballStartPos = playerStartPos + new Vector2((Player.Size.X / 2f) - BALL_RADIUS, -BALL_RADIUS * 2f);
			Ball = new BallObject(ballStartPos, BALL_RADIUS, ResourceManager.GetTex("ball"), Color.Pink.ToVector3(), velocity: new Vector2(GetRandomBallSpeed(), Difficulty.BallUpSpeed));
			Balls.Add(Ball);

			// instanciar gerador de partículas para efeito de "rastro"
			ParticleGenerator = new ParticleGenerator(ResourceManager.GetShader("particle"), ResourceManager.GetTex("particle"), 500);

			// instanciar classe responsável pelos efeitos de pós-processamento
			PostProcessor = new PostProcessor(ResourceManager.GetShader("postProcessing"), Width, Height);

			// iniciar renderizador de texto
			TextRenderer = new TextRenderer(Width, Height);
			TextRenderer.Load("fonts/ocraext.TTF", 24);

			// iniciar overlay para tela de "pause"
			Overlay = new Texture2D();
			Overlay.Generate(Width, Height, IntPtr.Zero);

			// setar a posição inicial do mouse para o meio da tela
			var point = PointToScreen(new Point(Width / 2, Height / 2));
			Mouse.SetPosition(point.X, point.Y);

			// estado inicial do jogo deve ser o menu
			State = GameState.Menu;
		}

		protected override void OnUpdateFrame(FrameEventArgs e)
		{
			if (State == GameState.Active)
			{
				base.OnUpdateFrame(e);
				ProcessEvents();

				// remover todas as bolas que saíram da tela
				Balls.RemoveAll(b => b.Position.Y >= Height);

				// condição de derrota
				if (Balls.Count == 0)
				{
					Lives--;
					if (Lives == 0)
					{
						State = GameState.Lost;
						PostProcessor.Chaos = false;
						PostProcessor.Confuse = true;
						return;
					}
					Balls.Add(Ball);
					ResetPlayer();
				}
				// caso contrário, selecionar a bola principal como uma das bolas restantes
				else
				{
					Ball = Balls[0];
					if (!Ball.Comet)
						Ball.Color = new Vector3(1);
				}

				// mover o jogador de acordo com a posição do mouse
				float x = Math.Max(Math.Min((float)(Player.Position.X + (PLAYER_VELOCITY * e.Time)), Width - Player.Size.X), 0);
				if (x <= 0 || x >= Width - Player.Size.X)
				{
					Player.Velocity = Vector2.Zero;
				}
				else
				{
					Player.Velocity = new Vector2((float)(PLAYER_VELOCITY * e.Time), 0);
				}
				Player.Position = new Vector2(x, Player.Position.Y);

				// mover todas as bolas existentes na tela
				Balls.ForEach(ball =>
				{
					ball.Move((float)e.Time, Width);
					if (ball.Stuck)
						ball.Position += Player.Velocity;
				});

				// atualizar a posição das partículas de rastro da bola principal
				ParticleGenerator.Update((float)e.Time, Ball, 2, new Vector2(Ball.Radius / 2f));

				// atualizar o estado de todos os powerups
				UpdatePowerUps((float)e.Time);

				// checar colisão
				DoCollisions();

				// atualizar o tempo restante do efeito de "shake"
				if (shakeTime > 0)
				{
					shakeTime -= (float)e.Time;
					if (shakeTime <= 0)
						PostProcessor.Shake = false;
				}

				score = Levels[Level].GetScore();
			}
		}

		protected override void OnRenderFrame(FrameEventArgs e)
		{
			base.OnRenderFrame(e);
			// limpar o buffer de cor do openGL
			GL.Clear(ClearBufferMask.ColorBufferBit);

			// encapsular a renderização de objetos para permitir efeitos de pós-processamento
			PostProcessor.BeginRender();
			//renderizar o background
			Renderer.DrawTexture(ResourceManager.GetTex("breakout_bg"), Vector2.Zero, new Vector2(Width, Height), 0, Vector3.One);
			// desenhar os "tijolos"
			Levels[Level].Draw(Renderer);
			//desenhar o jogador
			Player.Draw(Renderer);
			//desenhar as partículas de rastro
			ParticleGenerator.Draw();
			//desenhar todas as bolas
			Balls.ForEach(ball => ball.Draw(Renderer));
			//desenhar os powerups
			PowerUps.ForEach(p =>
			{
				if (!p.Destroyed)
					p.Draw(Renderer);
			});
			PostProcessor.EndRender();
			// aplicar efeitos de pós-processamento
			PostProcessor.Render((float)e.Time);

			// renderizar textos informativos na tela, dependendo do estado do jogo.
			// textos não devem passar pelos efeitos de pós processamento
			// suporte a acentuação não foi implementado
			if (State == GameState.Active || State == GameState.Paused)
			{
				TextRenderer.RenderText($"Vidas: {Lives}", 5, 5, 1);
				TextRenderer.RenderText("Efeitos:", 15, Height - 75, .95f);

				// instrução de pressionar espaço só deve ser exibida até que o jogador pressione espaço
				if (drawInstruction)
				{
					TextRenderer.RenderText("Pressione espaco", (Width / 2) - 120f, (Height / 2) + 80f, 1);
				}

				// desenhar os powerups que estão ativos no momento
				for (int i = 0; i < PowerUps.Count; i++)
				{
					if (PowerUps[i].Active)
						Renderer.DrawTexture(PowerUps[i].Sprite, new Vector2(30 + (i * PowerUps[i].Size.X), Height - (1.1f * PowerUps[i].Size.Y)), PowerUps[i].Size / 1.25f, 0, Color.DeepPink.ToVector3());
				}
			}
			if (State == GameState.Menu)
			{
				TextRenderer.RenderText("Pressione ENTER para iniciar", (Width / 2f) - 200, (Height / 2f) + 20f, 1);
				TextRenderer.RenderText("Pressione W ou S para selecionar a fase", (Width / 2f) - 225f, (Height / 2f) + 50f, .8f);
				TextRenderer.RenderText("Pressione I para INFO", (Width / 2f) - 95f, (Height / 2f) + 80f, .6f);
				TextRenderer.RenderText($"Dificuldade: {Difficulty.Name}", (Width / 2) - 110, Height * .7f, .8f);

				// tela informativa com instruções sobre como jogar
				if (showInfo)
				{
					Renderer.DrawTexture(Overlay, Vector2.Zero, new Vector2(Width, Height), 0, Color.DarkSlateGray.ToVector4(.8f));
					TextRenderer.RenderText("INFO", (Width / 2) - 75, 16, 2);
					TextRenderer.RenderText("Bem vindo ao TIJOLATOR!", 15, 16 * 5, .8f);
					TextRenderer.RenderText("Para mudar a dificuldade, pressione [1], [2] ou [3]", 15, 16 * 7.5f, .8f);
					TextRenderer.RenderText("O mouse movimenta a barra de acordo com sua posicao", 15, 16 * 9.5f, .8f);
					TextRenderer.RenderText("relativa ao centro da tela, sendo que quanto mais", 15, 16 * 11, .8f);
					TextRenderer.RenderText("perto da borda, mais velocidade naquela direcao a", 15, 16 * 12.5f, .8f);
					TextRenderer.RenderText("barra tem", 15, 16 * 14, .8f);
					TextRenderer.RenderText("O botao da esquerda pausa, e o da direita imprime ", 15, 16 * 17, .8f);
					TextRenderer.RenderText("informacoes tecnicas.", 15, 16 * 18.5f, .8f);
					TextRenderer.RenderText("Quando a bola grudar na barra,", 15, 16 * 20.5f, .8f);
					TextRenderer.RenderText("aperte espaco para solta-la", 15, 16 * 22, .8f);
					TextRenderer.RenderText("Por fim:", 15, 16 * 25, .8f);
					TextRenderer.RenderText("LET'S TIJOLATE!", (Width / 2) - 170, 16 * 32, 1.5f);
				}
			}
			if (State == GameState.Win)
			{
				TextRenderer.RenderText("Venceste!!!", (Width / 2f) - 75f, (Height / 2f) + 280f, 1f, new Vector3(0f, 1f, 0f));
			}
			if (State == GameState.Lost)
			{
				TextRenderer.RenderText("Perdeste! :(", (Width / 2f) - 75f, (Height / 2f) - 100f, 1f, new Vector3(1f, 0f, 0f));
				TextRenderer.RenderText("Pressione R para voltar ao menu", (Width / 2f) - 225f, (Height / 2f) - 70f, 1f, Vector3.Zero);
			}

			if (State != GameState.Menu)
				TextRenderer.RenderText($"Score: {score}", Width - 150, 5, 1, State == GameState.Lost ? Vector3.Zero : Vector3.One);

			// condição de vitória
			if (State == GameState.Active && Levels[Level].IsCompleted())
			{
				Levels[Level].Reload(Width, Height / 2);
				PostProcessor.Confuse = false;
				PostProcessor.Chaos = true;
				State = GameState.Win;
			}

			// pause decorrente do botão direito do mouse
			if (shouldPause)
			{
				State = GameState.Paused;
				shouldPause = false;
				LogInfo();
			}

			if (State == GameState.Paused)
			{
				Renderer.DrawTexture(Overlay, Vector2.Zero, new Vector2(Width, Height), 0, Color.DarkSlateGray.ToVector4(.4f));
				TextRenderer.RenderText("PAUSE", (Width / 2f) - 75, Height / 2f, 2);
				for (int line = 0; line < lines.Length; line++)
				{
					var height = line * TextRenderer.Characters['T'].Size.Y;
					bool secondColumn = height > Width - 15;
					TextRenderer.RenderText(lines[line], 5 + (secondColumn ? Width / 2 : 0), 5 + (secondColumn ? height - Height + 75 : height), .55f, Color.Yellow.ToVector3());
				}
			}

			Context.SwapBuffers();
		}

		// evento de ajuste do tamanho da tela, desabilitado
		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);
			GL.Viewport(0, 0, Width, Height);
		}

		// tratar teclas pressionadas
		protected override void OnKeyDown(KeyboardKeyEventArgs e)
		{
			base.OnKeyDown(e);
			// sair do jogo
			if (e.Key == Key.Q)
			{
				Close();
			}
			// reiniciar o jogo
			if (e.Key == Key.R)
			{
				Reset();
			}
			// soltar a bola do paddle
			if (State == GameState.Active && e.Key == Key.Space)
			{
				drawInstruction = false;
				Balls.ForEach(ball => ball.Stuck = false);
			}
			if (State == GameState.Menu)
			{
				// selecionar a fase com 'W' e 'S'
				if (e.Key == Key.W)
				{
					if (showInfo)
						showInfo = false;
					else
						Level = (Level + 1) % Levels.Count;
				}
				if (e.Key == Key.S)
				{
					if (showInfo)
						showInfo = false;
					else
						if (Level > 0)
							Level--;
					else
						Level = Levels.Count - 1;
				}
				// começar o jogo com a fase ativa
				if (e.Key == Key.Enter)
				{
					// quantidade de vidas de acordo com a dificuldade
					Lives = Difficulty.LifeAmount;

					drawInstruction = true;
					Ball.Velocity = new Vector2(Ball.Velocity.X, Difficulty.BallUpSpeed);
					if (showInfo)
						showInfo = false;
					else
						State = GameState.Active;
				}
				// mostrar/esconder informações
				if (e.Key == Key.I)
				{
					showInfo = !showInfo;
				}
			}
			// 'cheat' para pular para a tela de vitória
			if (e.Key == Key.L)
			{
				Levels[Level].Reload(Width, Height / 2);
				PostProcessor.Confuse = false;
				PostProcessor.Chaos = true;
				State = GameState.Win;
			}
			// 'cheat' para pular para a tela de derrota
			if (e.Key == Key.K)
			{
				PostProcessor.Chaos = false;
				PostProcessor.Confuse = true;
				State = GameState.Lost;
			}
			// alterar dificuldade
			if (e.Key == Key.Number1)
			{
				Difficulty = Difficulties[0];
			}
			if (e.Key == Key.Number2)
			{
				Difficulty = Difficulties[1];
			}
			if (e.Key == Key.Number3)
			{
				Difficulty = Difficulties[2];
			}
		}

		protected override void OnMouseMove(MouseMoveEventArgs e)
		{
			base.OnMouseMove(e);
			// atualizar velocidade do jogador com base na posição do mouse
			PLAYER_VELOCITY = (e.X - (Width / 2)) * Difficulty.PlayerSpeedMod;
		}

		//tratar pause 
		protected override void OnMouseDown(MouseButtonEventArgs e)
		{
			if (e.Button == MouseButton.Left)
			{
				if (State == GameState.Active)
				{
					State = GameState.Paused;
				}
				else if (State == GameState.Paused)
				{
					State = GameState.Active;
					lines = new string[] { };
				}
			}
			if (e.Button == MouseButton.Right)
			{
				if (State == GameState.Active)
				{
					State = GameState.Paused;
					LogInfo();
				}
				else if (State == GameState.Paused)
				{
					State = GameState.Active;
					shouldPause = true;
				}
			}
			base.OnMouseDown(e);
		}

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
		}

		private void DoCollisions()
		{
			// para cada bola ativa, verificar quais blocos colidem com a bola, 
			// e se alguma delas colide com o paddle ou não. Aplicar os efeitos da colisão
			Balls.ForEach(ball =>
			{
				foreach (GameObject box in Levels[Level].Bricks)
				{
					Collision col = ball.CheckCollision(box);
					if (!box.Destroyed && col.Colliding)
					{
						if (!box.IsSolid)
						{
							// quebrar o bloco e criar um powerup, se aplicável
							box.Destroyed = true;
							SpawnPowerUps(box);
							SoundEngine.Instance.PlaySound(ResourceManager.GetSound("bleepBrick"));
						}
						else
						{
							// aplicar efeito de "balanço" e "borrado" da tela
							PostProcessor.Shake = true;
							shakeTime = .05f;
							SoundEngine.Instance.PlaySound(ResourceManager.GetSound("solid"));
						}

						// refletir a bola na direção apropriada
						if (!ball.Comet || box.IsSolid)
						{
							// 'pen' representa o quanto a bola se moveu para dentro do bloco
							float pen;
							switch (col.Direction)
							{
								case Direction.Left:
								case Direction.Right:
									ball.Velocity = new Vector2(-ball.Velocity.X, ball.Velocity.Y);
									pen = ball.Radius - Math.Abs(col.Vector.X);
									if (col.Direction == Direction.Left)
										ball.Position = new Vector2(ball.Position.X + pen, ball.Position.Y);
									else
										ball.Position = new Vector2(ball.Position.X - pen, ball.Position.Y);
									break;
								default:
									ball.Velocity = new Vector2(ball.Velocity.X, -ball.Velocity.Y);
									pen = ball.Radius - Math.Abs(col.Vector.Y);
									if (col.Direction == Direction.Up)
										ball.Position = new Vector2(ball.Position.X, ball.Position.Y - pen);
									else
										ball.Position = new Vector2(ball.Position.X, ball.Position.Y + pen);
									break;
							}
							// evitar múltiplas colisões em um único frame, devido à erros de reflexão
							break;
						}
					}
				}

				Collision playerBallCol = ball.CheckCollision(Player);
				if (!ball.Stuck && playerBallCol.Colliding)
				{
					float paddleCenter = Player.Position.X + (Player.Size.X / 2);
					float distance = ball.Position.X + ball.Radius - paddleCenter;
					float percentage = distance / (Player.Size.X / 2);

					// refletir a bola mantendo a magnitude da velocidade
					float velocityMag = Ball.Velocity.Length;
					var relativeVelocity = new Vector2(BALL_X_SPEED * percentage * 2, -Math.Abs(ball.Velocity.Y));
					ball.Velocity = relativeVelocity.Normalized() * velocityMag;
					ball.Stuck = ball.Sticky;
					SoundEngine.Instance.PlaySound(ResourceManager.GetSound("bleepPaddle"));
				}
			});

			// verificar a colisão de powerups com o paddle e ativá-los se necessário
			foreach (PowerUp power in PowerUps)
			{
				if (!power.Destroyed)
				{
					if (power.Position.Y >= Height)
						power.Destroyed = true;
					if (Player.CheckCollision(power).Colliding)
					{
						power.Destroyed = true;
						ActivatePowerUp(power);
						power.Active = true;
						SoundEngine.Instance.PlaySound(ResourceManager.GetSound("powerup"));
					}
				}
			}
		}

		// aplicar efeitos específicos dependendo do tipo do powerup
		private void ActivatePowerUp(PowerUp power)
		{
			switch (power.Type)
			{
				case "speed":
					Balls.ForEach(ball => ball.Velocity *= 1.5f);
					break;
				case "sticky":
					// sticky faz a bola grudar no paddle quando colide
					Balls.ForEach(ball => ball.Sticky = true);
					Player.Color = Color.PaleGreen.ToVector3();
					break;
				case "comet":
					// comet faz a bola ignorar colisão com os 'tijolos'
					Balls.ForEach(ball =>
					{
						ball.Comet = true;
						ball.Color = Color.DarkRed.ToVector3();
					});
					break;
				case "padIncrease":
					Player.Size = new Vector2(Player.Size.X * 1.25f, Player.Size.Y);
					break;
				case "padDecrease":
					Player.Size = new Vector2(Player.Size.X * .75f, Player.Size.Y);
					break;
				case "confuse":
					PostProcessor.Confuse = true;
					break;
				case "chaos":
					PostProcessor.Chaos = true;
					break;
				case "multiply":
					SpawnBalls(Difficulty.AmountOfBalls);
					break;
			}
		}

		private void SpawnBalls(int amount)
		{
			// cria uma quantidade de bolas de acordo com a dificuldade
			// e aplica uma variação aleatória na direção horizontal dela
			for (int i = 0; i < amount; i++)
			{
				var ball = new BallObject(Ball.Position, Ball.Radius, Ball.Sprite, Color.Aqua.ToVector3(), new Vector2(Ball.Velocity.X + ((float)Util.Random.NextDouble() * 2f), Ball.Velocity.Y))
				{
					Stuck = false
				};
				Balls.Add(ball);
			}
		}

		private void SpawnPowerUps(GameObject brick)
		{
			// caso um double aleatório seja maior que a chance de criar um powerup 
			// da dificuldade atual, um powerup aleatório é gerado
			if (Util.Random.NextDouble() > Difficulty.PowerUpChance)
			{
				List<string> types = Difficulty.PowerUpTypes;
				Dictionary<string, float> duration = Difficulty.PowerUpDuration;
				string type = types[Util.Random.Next(0, types.Count)];
				PowerUps.Add(new PowerUp(type, Color.DeepPink.ToVector3(), duration[type], brick.Position, ResourceManager.GetTex(type)));
			}
		}

		private void UpdatePowerUps(float deltaTime)
		{
			// para cada powerup, atualizar a posição e a duração dele
			for (int i = 0; i < PowerUps.Count; i++)
			{
				PowerUps[i].Position += PowerUps[i].Velocity;
				if (PowerUps[i].Active)
				{
					PowerUps[i].Duration -= deltaTime;

					//se o powerup se tornar inativo, remover os efeitos que ele havia aplicado
					if (PowerUps[i].Duration <= 0)
					{
						PowerUps[i].Active = false;
						switch (PowerUps[i].Type)
						{
							case "speed":
								Balls.ForEach(ball => ball.Velocity /= 1.5f);
								break;
							case "sticky":
								bool isAnySticky = PowerUps.Any(p => p.Active && p.Type == "sticky");
								Balls.ForEach(ball => ball.Sticky = isAnySticky);
								if (!isAnySticky)
									Player.Color = new Vector3(1);
								break;
							case "comet":
								Balls.ForEach(ball =>
								{
									ball.Comet = PowerUps.Any(p => p.Active && p.Type == "comet");
									ball.Color = Color.Aqua.ToVector3();
								});
								Ball.Color = Vector3.One;
								break;
							case "padIncrease":
								Player.Size = new Vector2(Player.Size.X / 1.25f, Player.Size.Y);
								break;
							case "padDecrease":
								Player.Size = new Vector2(Player.Size.X / .75f, Player.Size.Y);
								break;
							case "confuse":
								PostProcessor.Confuse = PowerUps.Any(p => p.Active && p.Type == "confuse");
								break;
							case "chaos":
								PostProcessor.Chaos = PowerUps.Any(p => p.Active && p.Type == "chaos");
								break;
						}
					}
				}

				// remover powerups inativos
				if (!PowerUps[i].Active && PowerUps[i].Destroyed)
				{
					PowerUps.RemoveAt(i);
				}
			}
		}

		// retornar o jogo para as condições iniciais
		private void Reset()
		{
			State = GameState.Menu;
			Levels.Clear();
			UpdatePowerUps(100000f);
			PowerUps.Clear();
			PostProcessor.Chaos = false;
			PostProcessor.Confuse = false;
			Balls.Clear();
			Init();
		}

		// retornar o jogador, powerups, efeitos e bolas para o estado inicial
		// mas sem alterar o estado do nível atual
		private void ResetPlayer()
		{
			UpdatePowerUps(100000);
			PostProcessor.Chaos = false;
			PostProcessor.Confuse = false;
			PowerUps.Clear();
			Player.Position = new Vector2((Width / 2) - (PLAYER_SIZE.X / 2), Height - PLAYER_SIZE.Y - 84f);
			Player.Size = PLAYER_SIZE;
			Player.Color = new Vector3(1);
			Ball.Reset(Player.Position + new Vector2((Player.Size.X / 2) - BALL_RADIUS, -BALL_RADIUS * 2f), new Vector2(GetRandomBallSpeed(), Difficulty.BallUpSpeed));
			Ball.Size = new Vector2(2f * BALL_RADIUS);
		}

		// gravar informações a respeito dos objetos
		private void LogInfo()
		{
			StringBuilder sb = new StringBuilder();

			sb.Append("======================================");
			sb.Append('\n').Append("DEBUG INFORMATION");
			sb.Append('\n').Append("Difficulty");
			sb.Append('\n').Append("     Name: ").Append(Difficulty.Name);
			sb.Append('\n').Append("Paddle");
			sb.Append('\n').Append("     Position: ").Append(Player.Position);
			sb.Append('\n').Append("     Velocity: ").Append(Player.Velocity);
			sb.Append('\n').Append("     Size: ").Append(Player.Size);
			sb.Append('\n').Append("Balls");
			foreach (BallObject ball in Balls)
			{
				sb.Append('\n').Append("     Position: ").Append(ball.Position);
				sb.Append('\n').Append("     Velocity: ").Append(ball.Velocity);
				sb.Append('\n').Append("     Radius: ").Append(ball.Radius);
				sb.Append('\n').Append("     Comet: ").Append(ball.Comet);
				sb.Append('\n').Append("     Sticky: ").Append(ball.Sticky);
			}
			sb.Append('\n').Append("PostProcessing");
			sb.Append('\n').Append("     Chaos: ").Append(PostProcessor.Chaos);
			sb.Append('\n').Append("     Confuse: ").Append(PostProcessor.Confuse);
			sb.Append('\n').Append("     Shake: ").Append(PostProcessor.Shake);
			sb.Append('\n').Append("PowerUps");
			foreach (var p in PowerUps)
			{
				sb.Append('\n').Append("     Type: ").Append(p.Type);
				sb.Append('\n').Append("         Active: ").Append(p.Active);
				sb.Append('\n').Append("         Duration: ").Append(p.Duration);
			}
			sb.Append('\n').Append("Bricks");
			for (int i = 0; i < Levels[Level].Bricks.Count; i++)
			{
				var brick = Levels[Level].Bricks[i];
				if (!brick.Destroyed)
				{
					sb.Append('\n').Append("     Brick").Append(i);
					sb.Append('\n').Append("         Position").Append(brick.Position);
					sb.Append('\n').Append("         IsSolid").Append(brick.IsSolid);
				}
			}
			sb.Append('\n').Append("======================================");
			string text = sb.ToString();
			Console.WriteLine(text);

			lines = text.Split('\n');
		}

		// obter uma velocidade aleatória para o eixo X da bola.
		private float GetRandomBallSpeed()
		{
			if (Util.Random.NextDouble() > .5)
			{
				return 1 * BALL_X_SPEED;
			}
			return -1 * BALL_X_SPEED;
		}
	}
}
