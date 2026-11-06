namespace be.Domain.Identity;

// Administrative account state is separate from temporary failed-login lockout.
public enum CustomerAccountStatus { Active, Suspended, Blocked, PendingDeletion, Removed }
