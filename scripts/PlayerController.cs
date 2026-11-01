using Godot;
using System;

namespace NoExit
{
    /// <summary>
    /// NO EXIT - Player Controller
    /// کنترلر پلیر آرمین با قابلیت حرکت، تعامل و انیمیشن
    /// </summary>
    public partial class PlayerController : CharacterBody2D
    {
        #region Exports
        [Export] private float _speed = 200f;
        [Export] private float _friction = 0.15f;
        [Export] private float _interactionRange = 50f;
        [Export] private int _interactionLayer = 2; // Layer for interactable objects
        #endregion

        #region Private Fields
        private Vector2 _velocity = Vector2.Zero;
        private bool _canMove = true;
        private bool _isInteracting = false;

        // Animation
        private AnimatedSprite2D _animatedSprite;
        private Sprite2D _body;
        private Sprite2D _hood;

        // Interaction
        private Node2D _currentTarget;
        private RayCast2D _interactionRaycast;
        private Marker2D _interactionPoint;

        // References
        private GameManager _gameManager;
        #endregion

        #region Properties
        public bool CanMove
        {
            get => _canMove;
            set => _canMove = value;
        }
        #endregion

        #region Godot Lifecycle
        public override void _Ready()
        {
            // Get references
            _animatedSprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
            _body = GetNodeOrNull<Sprite2D>("Body");
            _hood = GetNodeOrNull<Sprite2D>("Hood");

            // Setup interaction raycast
            _interactionPoint = GetNodeOrNull<Marker2D>("InteractionPoint");
            if (_interactionPoint == null)
            {
                _interactionPoint = new Marker2D();
                _interactionPoint.Name = "InteractionPoint";
                _interactionPoint.Position = new Vector2(20, 0); // Slightly in front
                AddChild(_interactionPoint);
            }

            _interactionRaycast = GetNodeOrNull<RayCast2D>("InteractionRaycast");
            if (_interactionRaycast == null)
            {
                _interactionRaycast = new RayCast2D();
                _interactionRaycast.Name = "InteractionRaycast";
                _interactionRaycast.Position = _interactionPoint.Position;
                _interactionRaycast.TargetPosition = new Vector2(_interactionRange, 0);
                _interactionRaycast.CollisionMask = _interactionLayer;
                AddChild(_interactionRaycast);
            }

            // Get GameManager reference
            _gameManager = GetNodeOrNull<GameManager>("/root/GameManager");
            if (_gameManager == null)
            {
                GD.PrintErr("[PlayerController] GameManager not found!");
            }
            else
            {
                _gameManager.Player = this;
            }
        }

        public override void _PhysicsProcess(double delta)
        {
            if (!_canMove)
            {
                _velocity = Vector2.Zero;
                UpdateAnimation(Vector2.Zero);
                return;
            }

            HandleMovement();
            HandleInteraction();
            UpdateAnimation(Velocity);

            MoveAndSlide();
        }
        #endregion

        #region Movement
        private void HandleMovement()
        {
            Vector2 inputDirection = Vector2.Zero;

            // Get input direction
            if (Input.IsActionPressed("move_left"))
                inputDirection.X -= 1;
            if (Input.IsActionPressed("move_right"))
                inputDirection.X += 1;
            if (Input.IsActionPressed("move_up"))
                inputDirection.Y -= 1;
            if (Input.IsActionPressed("move_down"))
                inputDirection.Y += 1;

            // Normalize diagonal movement
            if (inputDirection.Length() > 1)
                inputDirection = inputDirection.Normalized();

            // Smooth velocity with lerp
            _velocity = _velocity.Lerp(inputDirection * _speed, _friction);
            Velocity = _velocity;
        }
        #endregion

        #region Interaction
        private void HandleInteraction()
        {
            // Check for interactable objects when E is pressed
            if (Input.IsActionJustPressed("interact"))
            {
                TryInteract();
            }

            // Update current target highlight
            UpdateTargetHighlight();
        }

        private void TryInteract()
        {
            _interactionRaycast.ForceRaycastUpdate();

            if (_interactionRaycast.IsColliding())
            {
                var collider = _interactionRaycast.GetCollider();
                if (collider is Node2D interactable)
                {
                    GD.Print($"[PlayerController] Interacting with: {interactable.Name}");

                    // Call the interact method if it exists
                    if (interactable.HasMethod("OnInteract"))
                    {
                        interactable.Call("OnInteract", this);
                    }

                    // Emit signal for other systems
                    EmitSignal(SignalName.InteractionAttempted, interactable);
                }
            }
        }

        private void UpdateTargetHighlight()
        {
            _interactionRaycast.ForceRaycastUpdate();

            if (_interactionRaycast.IsColliding())
            {
                var collider = _interactionRaycast.GetCollider();
                if (collider is Node2D interactable && collider != _currentTarget)
                {
                    _currentTarget = interactable;
                    EmitSignal(SignalName.TargetChanged, interactable);
                }
            }
            else if (_currentTarget != null)
            {
                _currentTarget = null;
                EmitSignal(SignalName.TargetChanged, null);
            }
        }
        #endregion

        #region Animation
        private void UpdateAnimation(Vector2 velocity)
        {
            if (_animatedSprite == null) return;

            // Determine animation state
            if (velocity.Length() > 10)
            {
                _animatedSprite.Play("walk");

                // Flip based on direction
                if (velocity.X < 0)
                    _animatedSprite.FlipH = true;
                else if (velocity.X > 0)
                    _animatedSprite.FlipH = false;
            }
            else
            {
                _animatedSprite.Play("idle");
            }
        }

        /// <summary>
        /// پخش انیمیشن خاص
        /// </summary>
        public void PlayAnimation(string animationName)
        {
            if (_animatedSprite != null && _animatedSprite.HasAnimation(animationName))
            {
                _animatedSprite.Play(animationName);
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// تنظیم قابلیت حرکت
        /// </summary>
        public void SetCanMove(bool value)
        {
            _canMove = value;
            if (!value)
            {
                _velocity = Vector2.Zero;
                Velocity = Vector2.Zero;
            }
        }

        /// <summary>
        /// نمایش پیام تعامل
        /// </summary>
        public void ShowInteractionPrompt(string message)
        {
            // TODO: Connect to UI system
            GD.Print($"[Prompt] {message}");
        }
        #endregion

        #region Signals
        [Signal]
        public delegate void InteractionAttemptedEventHandler(Node2D target);

        [Signal]
        public delegate void TargetChangedEventHandler(Node2D newTarget);
        #endregion
    }
}
