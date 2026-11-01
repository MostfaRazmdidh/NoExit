using Godot;

namespace NoExit
{
    /// <summary>
    /// NO EXIT - Interactable Object Base
    /// کلاس پایه برای تمام اشیای قابل تعامل در بازی
    /// </summary>
    public abstract partial class InteractableObject : Area2D
    {
        #region Properties
        [Export] public string ObjectId { get; set; } = "";
        [Export] public string DisplayName { get; set; } = "Object";
        [Export] public string Description { get; set; } = "";
        [Export] public bool IsInteractable { get; set; } = true;
        [Export] public bool IsHighlighted { get; set; } = false;
        [Export] public bool RequiresItem { get; set; } = false;
        [Export] public string RequiredItemId { get; set; } = "";
        #endregion

        #region Signals
        [Signal]
        public delegate void InteractedEventHandler(InteractableObject source, PlayerController player);

        [Signal]
        public delegate void DescriptionShownEventHandler(string description);
        #endregion

        public override void _Ready()
        {
            // Connect to area entered/exited for hover effects
            AreaEntered += OnAreaEntered;
            AreaExited += OnAreaExited;
        }

        #region Virtual Methods
        /// <summary>
        /// وقتی بازیکن با شیء تعامل می‌کند
        /// </summary>
        public virtual void OnInteract(PlayerController player)
        {
            if (!IsInteractable) return;

            // Check if item is required
            if (RequiresItem)
            {
                if (!GameManager.Instance.HasItem(RequiredItemId))
                {
                    // Show need item message
                    ShowDescription($"به {DisplayName} نیاز داری...");
                    return;
                }
            }

            GD.Print($"[InteractableObject] {DisplayName} interacted by player");
            EmitSignal(SignalName.Interacted, this, player);

            // Call subclass specific behavior
            PerformInteraction(player);
        }

        /// <summary>
        /// رفتار خاص هر شیء - باید در کلاس‌های فرزند پیاده‌سازی شود
        /// </summary>
        protected virtual void PerformInteraction(PlayerController player)
        {
            // Default implementation - override in subclasses
        }

        /// <summary>
        /// نمایش توضیحات شیء
        /// </summary>
        public virtual void ShowDescription(string customText = "")
        {
            string text = string.IsNullOrEmpty(customText) ? Description : customText;
            if (!string.IsNullOrEmpty(text))
            {
                EmitSignal(SignalName.DescriptionShown, text);
                GD.Print($"[InteractableObject] {DisplayName}: {text}");
            }
        }
        #endregion

        #region Hover Effects
        private void OnAreaEntered(Area2D area)
        {
            if (area is PlayerController)
            {
                IsHighlighted = true;
                OnHoverStart();
            }
        }

        private void OnAreaExited(Area2D area)
        {
            if (area is PlayerController)
            {
                IsHighlighted = false;
                OnHoverEnd();
            }
        }

        protected virtual void OnHoverStart()
        {
            // Override for hover start effect (glow, scale, etc.)
            GD.Print($"[InteractableObject] {DisplayName} hover start");
        }

        protected virtual void OnHoverEnd()
        {
            // Override for hover end effect
            GD.Print($"[InteractableObject] {DisplayName} hover end");
        }
        #endregion
    }
}
