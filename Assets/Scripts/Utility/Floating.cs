using UnityEngine;

namespace Utility
{
    /// <summary>
    /// This class makes the attached object float up and down in a sine wave pattern
    /// </summary>
    public class Floating : MonoBehaviour
    {
        [Header("Settings")] 
        [Tooltip("The speed to float at")]
        public float speed = 1.0f;
        [Tooltip("The height to float at")] 
        public float height = 0.5f;

        private Vector3 _startPosition = Vector3.zero;

        /// <summary>
        /// Description:
        /// Standard Unity function called when the object is first created
        /// Inputs: 
        /// none
        /// Returns: 
        /// void (no return)
        /// </summary>
        private void Start()
        {
            _startPosition = transform.position;
        }

        /// <summary>
        /// Description:
        /// Standard Unity function called after all Update functions have been called
        /// (good for things like camera movement and in this case floating)
        /// Inputs: 
        /// none
        /// Returns: 
        /// void (no return)
        /// </summary>
        private void LateUpdate()
        {
            Float();
        }

        /// <summary>
        /// Description:
        /// Floats this object up and down in a sine wave pattern
        /// Inputs: 
        /// none
        /// Returns: 
        /// void (no return)
        /// </summary>
        private void Float()
        {
            transform.position = _startPosition + Vector3.up * Mathf.Sin(Time.time * speed) * height;
        }
    }
}