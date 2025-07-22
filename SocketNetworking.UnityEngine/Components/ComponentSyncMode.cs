namespace SocketNetworking.UnityEngine.Components
{
    /// <summary>
    /// Defines the Sync mode for a component.
    /// </summary>
    public enum ComponentSyncMode
    {
        /// <summary>
        /// The Component will not check or be alerted for changes, instead it will wait for changes from external code.
        /// </summary>
        Manual = 0,
        /// <summary>
        /// Called 50 times per second.
        /// </summary>
        PhysicsUpdate = 1,
        /// <summary>
        /// Called every frame.
        /// </summary>
        FrameUpdate = 2,
        /// <summary>
        /// Called via patches if supported.
        /// </summary>
        Automatic = 3,
    }
}
