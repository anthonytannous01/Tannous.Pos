namespace Tannous.Pos.Domain.Enums;

public enum ReservationStatus
{
    Pending   = 0,  // created, awaiting confirmation
    Confirmed = 1,  // confirmed by staff
    Seated    = 2,  // customer arrived and seated
    Cancelled = 3,  // cancelled by customer or staff
    NoShow    = 4   // customer did not arrive
}
