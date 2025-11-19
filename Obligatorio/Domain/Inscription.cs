using System;

namespace Domain
{
    public enum InscriptionStatus
    {
        Active,
        Cancelled,
        Finished
    }

    public class Inscription
    {
        private static int _nextId = 1;
        public int Id { get; private set; }
        public User User { get; private set; }
        public OnlineClass Class { get; private set; }
        public DateTimeOffset EnrollmentDate { get; private set; }
        public InscriptionStatus Status { get; private set; }
        public string? WebhookUrl { get; private set; }
        public bool NotificationSent { get; set; } = false;

        public Inscription(User user, OnlineClass onlineClass, string? webhookUrl = null)
        {
            Id = _nextId++;
            User = user ?? throw new ArgumentNullException(nameof(user));
            Class = onlineClass ?? throw new ArgumentNullException(nameof(onlineClass));
            EnrollmentDate = DateTimeOffset.Now;
            Status = InscriptionStatus.Active;
            WebhookUrl = webhookUrl;
        }

        public void Cancel()
        {
            Status = InscriptionStatus.Cancelled;
        }
    }
}