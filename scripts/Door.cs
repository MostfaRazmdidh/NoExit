using Godot;

namespace NoExit
{
    /// <summary>
    /// NO EXIT - Door System
    /// سیستم درها با قفل و کلید
    /// </summary>
    public partial class Door : InteractableObject
    {
        #region Enums
        public enum DoorState { Locked, Unlocked, Open, Broken }
        public enum LockType { None, Key, Puzzle, Story }
        #endregion

        #region Exports
        [Export] public DoorState CurrentState { get; private set; } = DoorState.Locked;
        [Export] public LockType RequiredLockType { get; set; } = LockType.None;
        [Export] public string RequiredKeyId { get; set; } = "";
        [Export] public string RequiredPuzzleId { get; set; } = "";
        [Export] public string TargetScene { get; set; } = "";
        [Export] public Vector2 TargetPosition { get; set; } = new Vector2(576, 588);
        [Export] public bool IsOneWay { get; set; } = false;
        #endregion

        #region Node References
        private Sprite2D _doorSprite;
        private Polygon2D _doorPolygon;
        private Polygon2D _framePolygon;
        private CollisionShape2D _collisionShape;
        private LightOccluder2D _lightOccluder;
        private AudioStreamPlayer2D _openSound;
        private AudioStreamPlayer2D _lockedSound;
        #endregion

        public override void _Ready()
        {
            base._Ready();

            // Setup node references
            _doorPolygon = GetNodeOrNull<Polygon2D>("DoorPolygon");
            _framePolygon = GetNodeOrNull<Polygon2D>("FramePolygon");
            _collisionShape = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
            _lightOccluder = GetNodeOrNull<LightOccluder2D>("LightOccluder2D");

            // Try to get sounds
            _openSound = GetNodeOrNull<AudioStreamPlayer2D>("OpenSound");
            _lockedSound = GetNodeOrNull<AudioStreamPlayer2D>("LockedSound");

            // Update initial visual state
            UpdateVisuals();
        }

        protected override void PerformInteraction(PlayerController player)
        {
            switch (CurrentState)
            {
                case DoorState.Locked:
                    TryUnlock(player);
                    break;

                case DoorState.Unlocked:
                    OpenDoor(player);
                    break;

                case DoorState.Open:
                    // Door is already open, maybe transition to next scene?
                    if (!string.IsNullOrEmpty(TargetScene))
                    {
                        TransitionToScene();
                    }
                    break;

                case DoorState.Broken:
                    ShowDescription("این در شکسته است...");
                    break;
            }
        }

        #region Lock/Unlock
        private void TryUnlock(PlayerController player)
        {
            bool canUnlock = false;
            string failMessage = "";

            switch (RequiredLockType)
            {
                case LockType.None:
                    canUnlock = true;
                    break;

                case LockType.Key:
                    if (GameManager.Instance.HasItem(RequiredKeyId))
                    {
                        canUnlock = true;
                    }
                    else
                    {
                        failMessage = "این در به یک کلید نیاز دارد...";
                    }
                    break;

                case LockType.Puzzle:
                    if (GameManager.Instance.IsPuzzleSolved(RequiredPuzzleId))
                    {
                        canUnlock = true;
                    }
                    else
                    {
                        failMessage = "یک معما این در را قفل کرده است...";
                    }
                    break;

                case LockType.Story:
                    // Story-based unlock - always can unlock after certain point
                    canUnlock = true;
                    break;
            }

            if (canUnlock)
            {
                CurrentState = DoorState.Unlocked;
                UpdateVisuals();
                ShowDescription("قفل باز شد!");

                // Play unlock sound
                PlaySound(_openSound);

                // Auto open after short delay
                var timer = new Timer();
                timer.WaitTime = 0.5;
                timer.OneShot = true;
                timer.Timeout += () => OpenDoor(player);
                AddChild(timer);
                timer.Start();
            }
            else
            {
                ShowDescription(failMessage);
                PlaySound(_lockedSound);

                // Shake effect for feedback
                ShakeDoor();
            }
        }

        public void Unlock()
        {
            if (CurrentState == DoorState.Locked)
            {
                CurrentState = DoorState.Unlocked;
                UpdateVisuals();
            }
        }
        #endregion

        #region Open/Close
        private void OpenDoor(PlayerController player)
        {
            if (CurrentState != DoorState.Unlocked) return;

            CurrentState = DoorState.Open;
            UpdateVisuals();
            PlaySound(_openSound);

            // Disable collision when open
            if (_collisionShape != null)
            {
                _collisionShape.Disabled = true;
            }

            // Store player spawn position for next room
            if (!string.IsNullOrEmpty(TargetScene))
            {
                GameManager.Instance.PlayerSpawnPosition = TargetPosition;
                TransitionToScene();
            }
        }

        private void TransitionToScene()
        {
            if (string.IsNullOrEmpty(TargetScene)) return;

            GD.Print($"[Door] Transitioning to: {TargetScene}");
            // TODO: Implement scene transition
            // GetTree().ChangeSceneToFile(TargetScene);
        }
        #endregion

        #region Visuals
        private void UpdateVisuals()
        {
            Color doorColor = Colors.Black;

            switch (CurrentState)
            {
                case DoorState.Locked:
                    doorColor = new Color(0.07f, 0.07f, 0.08f); // Dark
                    break;

                case DoorState.Unlocked:
                    doorColor = new Color(0.2f, 0.18f, 0.15f); // Slightly lighter
                    break;

                case DoorState.Open:
                    doorColor = new Color(0.05f, 0.05f, 0.05f, 0.5f); // Almost invisible
                    break;

                case DoorState.Broken:
                    doorColor = new Color(0.15f, 0.1f, 0.1f); // Reddish
                    break;
            }

            if (_doorPolygon != null)
            {
                _doorPolygon.Color = doorColor;
            }
        }

        private void ShakeDoor()
        {
            // Simple shake animation
            var tween = CreateTween();
            var originalPos = Position;

            tween.TweenProperty(this, "position", originalPos + new Vector2(5, 0), 0.05f);
            tween.TweenProperty(this, "position", originalPos + new Vector2(-5, 0), 0.05f);
            tween.TweenProperty(this, "position", originalPos + new Vector2(3, 0), 0.05f);
            tween.TweenProperty(this, "position", originalPos, 0.05f);
        }

        private void PlaySound(AudioStreamPlayer2D soundPlayer)
        {
            if (soundPlayer != null && soundPlayer.Stream != null)
            {
                soundPlayer.Play();
            }
        }
        #endregion

        #region Hover Effects
        protected override void OnHoverStart()
        {
            base.OnHoverStart();

            // Scale up slightly
            var tween = CreateTween();
            tween.TweenProperty(this, "scale", new Vector2(1.05f, 1.05f), 0.1f);
        }

        protected override void OnHoverEnd()
        {
            base.OnHoverEnd();

            // Scale back to normal
            var tween = CreateTween();
            tween.TweenProperty(this, "scale", new Vector2(1.0f, 1.0f), 0.1f);
        }
        #endregion
    }
}
