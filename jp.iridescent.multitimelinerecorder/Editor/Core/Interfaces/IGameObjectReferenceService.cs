using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.MultiTimelineRecorder;

namespace MultiTimelineRecorder.Core.Interfaces
{
    /// <summary>
    /// Interface for GameObject reference management service
    /// </summary>
    public interface IGameObjectReferenceService : IDisposable
    {
        /// <summary>
        /// Create a reference to a GameObject
        /// </summary>
        /// <param name="gameObject">The GameObject to reference</param>
        /// <returns>A serializable reference to the GameObject</returns>
        GameObjectReference CreateReference(GameObject gameObject);
        
        /// <summary>
        /// Create a reference to a Transform's GameObject
        /// </summary>
        /// <param name="transform">The Transform whose GameObject to reference</param>
        /// <returns>A serializable reference to the GameObject</returns>
        GameObjectReference CreateReference(Transform transform);
        
        /// <summary>
        /// Try to restore a GameObject from a reference
        /// </summary>
        /// <param name="reference">The reference to restore</param>
        /// <param name="gameObject">The restored GameObject if successful</param>
        /// <returns>True if restoration was successful</returns>
        bool TryRestoreReference(GameObjectReference reference, out GameObject gameObject);
        
        /// <summary>
        /// Try to restore a Transform from a reference
        /// </summary>
        /// <param name="reference">The reference to restore</param>
        /// <param name="transform">The restored Transform if successful</param>
        /// <returns>True if restoration was successful</returns>
        bool TryRestoreTransform(GameObjectReference reference, out Transform transform);
        
        /// <summary>
        /// Validate that a reference can be restored
        /// </summary>
        /// <param name="reference">The reference to validate</param>
        /// <exception cref="ArgumentNullException">If reference is null</exception>
        /// <exception cref="ReferenceRestoreException">If reference cannot be restored</exception>
        void ValidateReference(GameObjectReference reference);
        
        /// <summary>
        /// Validate multiple references
        /// </summary>
        /// <param name="references">The references to validate</param>
        /// <exception cref="ArgumentNullException">If references is null</exception>
        /// <exception cref="ReferenceRestoreException">If any reference cannot be restored</exception>
        void ValidateReferences(IEnumerable<GameObjectReference> references);
        
        /// <summary>
        /// Refresh all cached references (useful after scene changes)
        /// </summary>
        void RefreshAllReferences();
        
        /// <summary>
        /// Clear the reference cache
        /// </summary>
        void ClearCache();
    }
}