namespace InteractR.Notifications;

/// <summary>
/// Response from notification handler indicating the processing result
/// </summary>
public enum ENotificationResponse
{
    /// <summary>
    /// Handler chose to ignore the notification (e.g., already processed)
    /// </summary>
    Ignored,
    
    /// <summary>
    /// Handler processed the notification successfully
    /// </summary>
    Completed,
    
    /// <summary>
    /// Handler failed to process the notification
    /// </summary>
    Failed
}