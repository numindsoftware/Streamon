namespace Streamon.Subscription;

/// <summary>
/// Optional marker interface for projection state types that wish to record the
/// <see cref="Event.GlobalPosition"/> of the last event applied to them.
/// </summary>
/// <remarks>
/// When a projection state implements this interface, the projection store will
/// automatically stamp <see cref="ProjectionTrackingPosition"/> on every write,
/// and projectors can use it to skip events that have already been applied
/// (idempotent projection under at-least-once delivery).
/// </remarks>
public interface IProjectionTrackable
{
    /// <summary>
    /// The <see cref="StreamPosition.Value"/> of the last <see cref="Event.GlobalPosition"/>
    /// applied to this projection. Defaults to <c>0</c> for new projections.
    /// </summary>
    long ProjectionTrackingPosition { get; set; }
}
