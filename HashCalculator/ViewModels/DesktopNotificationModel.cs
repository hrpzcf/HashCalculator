using System;

namespace HashCalculator
{
    internal class DesktopNotificationModel : NotifiableModel
    {
        private string _notificationMessage;
        private NotificationType _notificationType;

        public NotificationType Type
        {
            get => this._notificationType;
            set => this.SetPropNotify(ref this._notificationType, value);
        }

        public string Message
        {
            get => this._notificationMessage;
            set => this.SetPropNotify(ref this._notificationMessage, value);
        }

        public DateTime Timestamp { get; } = DateTime.Now;
    }
}
