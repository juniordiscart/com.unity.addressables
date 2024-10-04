using System;


#if ENABLE_CCD
/// <summary>
/// CcdManager is a static class used to determine where to point Addressables when loading resources stored in CCD.
/// </summary>
public static class CcdManager
{
    /// <summary>
    /// Name of the environment that the project should use.
    /// </summary>
    public static string EnvironmentName { get; set; }
    /// <summary>
    /// Id of the bucket that the project should use.
    /// </summary>
    public static string BucketId { get; set; }
    /// <summary>
    /// Name of the badge the project should use.
    /// </summary>
    public static string Badge { get; set; }

    /// <summary>
    /// Determines if the CcdManager has been configured
    /// </summary>
    /// <returns>True if all fields have been set. False, otherwise.</returns>
    public static bool IsConfigured()
    {
        return !string.IsNullOrEmpty(EnvironmentName) && !string.IsNullOrEmpty(BucketId) && !string.IsNullOrEmpty(Badge);
    }
}
#endif
