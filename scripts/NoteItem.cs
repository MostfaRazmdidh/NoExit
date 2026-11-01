using Godot;

namespace NoExit
{
    /// <summary>
    /// NO EXIT - Note Item
    /// آیتم یادداشت و پرونده برای داستان‌گویی
    /// </summary>
    public partial class NoteItem : InteractableObject
    {
        #region Properties
        [Export] public string NoteText { get; set; } = "";
        [Export] public string NoteTitle { get; set; } = "";
        [Export] public bool IsRead { get; private set; } = false;
        [Export] public bool AddToInventoryOnRead { get; set; } = false;
        [Export] public string InventoryId { get; set; } = "";
        #endregion

        #region Node References
        private Sprite2D _paperSprite;
        private Polygon2D _paperPolygon;
        private AudioStreamPlayer2D _pickupSound;
        private Light2D _noteGlow;
        #endregion

        public override void _Ready()
        {
            base._Ready();

            // Setup node references
            _paperPolygon = GetNodeOrNull<Polygon2D>("Paper");
            _noteGlow = GetNodeOrNull<Light2D>("NoteGlow");
            _pickupSound = GetNodeOrNull<AudioStreamPlayer2D>("PickupSound");

            // Apply visual style
            UpdateVisuals();
        }

        protected override void PerformInteraction(PlayerController player)
        {
            if (IsRead && AddToInventoryOnRead)
            {
                // Already read, just show text again
                ShowNoteContent();
                return;
            }

            // First time reading
            IsRead = true;
            UpdateVisuals();

            // Play pickup sound
            if (_pickupSound != null && _pickupSound.Stream != null)
            {
                _pickupSound.Play();
            }

            // Add to inventory if needed
            if (AddToInventoryOnRead && !string.IsNullOrEmpty(InventoryId))
            {
                GameManager.Instance.AddToInventory(InventoryId);
            }

            // Show note content
            ShowNoteContent();

            // Emit signal for game events
            EmitSignal(SignalName.NoteRead, this);
        }

        private void ShowNoteContent()
        {
            if (!string.IsNullOrEmpty(NoteText))
            {
                // For now, just print to console
                // Later this will connect to UI system
                GD.Print($"\n========== {NoteTitle} ==========");
                GD.Print(NoteText);
                GD.Print("================================\n");

                // Show in game
                GameManager.Instance.ShowStoryText(NoteTitle + "\n" + NoteText, 5.0f);
            }
        }

        private void UpdateVisuals()
        {
            // Visual feedback for read/unread state
            if (_paperPolygon != null)
            {
                if (IsRead)
                {
                    // Faded appearance when read
                    _paperPolygon.Modulate = new Color(0.7f, 0.7f, 0.7f, 0.8f);
                }
                else
                {
                    // Full brightness - slight glow to attract attention
                    _paperPolygon.Modulate = new Color(1.0f, 1.0f, 1.0f, 1.0f);
                }
            }

            // Disable glow when read
            if (_noteGlow != null)
            {
                _noteGlow.Visible = !IsRead;
            }
        }

        protected override void OnHoverStart()
        {
            base.OnHoverStart();

            // Glow effect on hover
            if (_noteGlow != null && !IsRead)
            {
                _noteGlow.Visible = true;
            }

            // Scale up
            var tween = CreateTween();
            tween.TweenProperty(this, "scale", new Vector2(1.1f, 1.1f), 0.1f);

            // Show interaction hint
            string hint = IsRead ? $"مشاهده {NoteTitle}" : $"خواندن {NoteTitle}";
            ShowDescription(hint);
        }

        protected override void OnHoverEnd()
        {
            base.OnHoverEnd();

            if (_noteGlow != null && !IsRead)
            {
                _noteGlow.Visible = false;
            }

            // Scale back
            var tween = CreateTween();
            tween.TweenProperty(this, "scale", new Vector2(1.0f, 1.0f), 0.1f);
        }

        #region Signal
        [Signal]
        public delegate void NoteReadEventHandler(NoteItem note);
        #endregion
    }
}
